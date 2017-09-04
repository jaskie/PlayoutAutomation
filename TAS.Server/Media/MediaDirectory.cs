using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Serialization;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common;
using TAS.Database;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    [DebuggerDisplay("{DirectoryName} ({_folder})")]
    public abstract class MediaDirectory : DtoBase, IMediaDirectory
    { 
        private FileSystemWatcher _watcher;
        private bool _isInitialized;
        private long _volumeFreeSize;
        private long _volumeTotalSize;
        private string _folder;


        protected readonly Dictionary<Guid, MediaBase> Files = new Dictionary<Guid, MediaBase>();
        protected readonly NLog.Logger Logger;
        internal MediaManager MediaManager;

        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;
        public event EventHandler<MediaEventArgs> MediaVerified;
        public event EventHandler<MediaEventArgs> MediaDeleted;
        internal event EventHandler<MediaPropertyChangedEventArgs> MediaPropertyChanged;
        
        protected MediaDirectory(MediaManager mediaManager)
        {
            MediaManager = mediaManager;
            Logger = NLog.LogManager.GetLogger(this.GetType().Name);
        }

#if DEBUG
        ~MediaDirectory()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        [XmlIgnore, JsonProperty]
        public virtual long VolumeFreeSize
        {
            get { return _volumeFreeSize; }
            protected set
            {
                if (_volumeFreeSize != value)
                {
                    _volumeFreeSize = value;
                    NotifyPropertyChanged(nameof(VolumeFreeSize));
                }
            }
        }

        [XmlIgnore, JsonProperty]
        public virtual long VolumeTotalSize
        {
            get { return _volumeTotalSize; }
            protected set
            {
                if (_volumeTotalSize != value)
                {
                    _volumeTotalSize = value;
                    NotifyPropertyChanged(nameof(VolumeTotalSize));
                }
            }
        }

        [JsonProperty]
        public string Folder
        {
            get { return _folder; }
            set { SetField(ref _folder, value); }
        }

        [JsonProperty]
        public virtual char PathSeparator => Path.DirectorySeparatorChar;

        [XmlIgnore, JsonProperty]
        public bool IsInitialized
        {
            get { return _isInitialized; }
            protected set { SetField(ref _isInitialized, value); }
        }

        [JsonProperty]
        public string DirectoryName { get; set; }


        public abstract IMedia CreateMedia(IMediaProperties mediaProperties);

        public virtual void Initialize()
        {
            if (!_isInitialized)
            {
                BeginWatch("*", false, TimeSpan.Zero); 
            }
        }

        public virtual void UnInitialize()
        {
            CancelBeginWatch();
            ClearFiles();
            IsInitialized = false;
        }

        public bool DirectoryExists()
        {
            return Directory.Exists(Folder);
        }
     
        public abstract void Refresh();

        public virtual IEnumerable<IMedia> GetFiles()
        {
            lock (((IDictionary)Files).SyncRoot)
                return Files.Values.Cast<IMedia>().ToList().AsReadOnly();
        }
        
        public virtual bool FileExists(string filename, string subfolder = null)
        {
            return File.Exists(Path.Combine(_folder, subfolder ?? string.Empty, filename));
        }

        public virtual bool DeleteMedia(IMedia media)
        {
            if (media.Directory == this)
            {
                string fullPath = ((MediaBase)media).FullPath;
                bool isLastWithTheName;
                lock (((IDictionary)Files).SyncRoot)
                    isLastWithTheName = !Files.Values.Any(m => m.FullPath.Equals(fullPath, StringComparison.CurrentCultureIgnoreCase) && m != media);
                if (isLastWithTheName && media.FileExists())
                {
                    try
                    {
                        File.Delete(((MediaBase)media).FullPath);
                        Debug.WriteLine(media, "File deleted");
                        return true;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("MediaDirectory.DeleteMedia {0} failed with error {1}", media, e.Message);
                    }
                }
                else
                {
                    MediaRemove(media);
                    NotifyMediaDeleted(media);
                    return true;
                }
            }
            return false;
        }

        public abstract void SweepStaleMedia();

        public virtual MediaBase FindMediaByMediaGuid(Guid mediaGuid)
        {
            MediaBase result;
            lock (((IDictionary)Files).SyncRoot)
                Files.TryGetValue(mediaGuid, out result);
            return result;
        }

        public virtual List<IMedia> FindMediaList(Func<IMedia, bool> condition)
        {
            lock (((IDictionary)Files).SyncRoot)
                return Files.Values.Where(condition).ToList();
        }

        public virtual IMedia FindMediaFirst(Func<IMedia, bool> condition)
        {
            lock (((IDictionary)Files).SyncRoot)
                return Files.Values.FirstOrDefault(condition);
        }

        internal void UpdateMediaGuid(Guid oldGuid, MediaBase media)
        {
            if (oldGuid != media.MediaGuid)
            {
                lock (((IDictionary) Files).SyncRoot)
                {
                    if (Files.ContainsKey(oldGuid))
                        Files.Remove(oldGuid);
                    Files.Add(media.MediaGuid, media);
                }
            }
        }

        internal virtual void NotifyMediaVerified(IMedia media)
        {
            MediaVerified?.Invoke(this, new MediaEventArgs(media));
        }

        protected virtual void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            IEnumerable<FileSystemInfo> list = new DirectoryInfo(directory).EnumerateFiles(string.IsNullOrWhiteSpace(filter) ? "*" : $"*{filter}*");
            foreach (var f in list)
            {
                if (cancelationToken.IsCancellationRequested)
                    return;
                AddFile(f.FullName, f.LastWriteTimeUtc);
            }
            if (!includeSubdirectories) return;
            list = new DirectoryInfo(directory).EnumerateDirectories();
            foreach (var d in list)
                EnumerateFiles(d.FullName, filter, true, cancelationToken);
        }

        private string _watcherFilter;
        private TimeSpan _watcherTimeout;
        private bool _watcherIncludeSubdirectories;
        private CancellationTokenSource _watcherTaskCancelationTokenSource;
        private System.Threading.Tasks.Task _watcherSetupTask;
        protected void BeginWatch(string filter, bool includeSubdirectories, TimeSpan timeout)
        {
            var oldTask = _watcherSetupTask;
            if (oldTask != null && oldTask.Status != System.Threading.Tasks.TaskStatus.RanToCompletion)
                return;
            var watcherTaskCancelationTokenSource = new CancellationTokenSource();
            _watcherSetupTask = System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                {
                    _watcherFilter = filter;
                    _watcherTimeout = timeout;
                    _watcherIncludeSubdirectories = includeSubdirectories;
                    bool watcherReady = false;
                    while (!watcherReady && !watcherTaskCancelationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            if (_watcher != null)
                            {
                                _watcher.Dispose();
                                _watcher = null;
                            }
                            if (Directory.Exists(_folder))
                            {
                                GetVolumeInfo();
                                EnumerateFiles(_folder, filter, includeSubdirectories, watcherTaskCancelationTokenSource.Token);
                                _watcher = new FileSystemWatcher(_folder)
                                {
                                    Filter = string.IsNullOrWhiteSpace(filter)
                                        ? string.Empty
                                        : $"*{filter}*",
                                    IncludeSubdirectories = includeSubdirectories,
                                    EnableRaisingEvents = true
                                };
                                _watcher.Created += OnFileCreated;
                                _watcher.Deleted += OnFileDeleted;
                                _watcher.Renamed += OnFileRenamed;
                                _watcher.Changed += OnFileChanged;
                                _watcher.Error += OnError;
                                watcherReady = _watcher.EnableRaisingEvents;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "Directory {0} watcher setup error", _folder);
                        }
                        if (!watcherReady)
                            Thread.Sleep(30000); //Wait for retry 30 sec.
                    }
                    if (watcherReady)
                        Debug.WriteLine("MediaDirectory: Watcher {0} setup successful.", (object)_folder);
                    else
                    if (watcherTaskCancelationTokenSource.IsCancellationRequested)
                    {
                        Debug.WriteLine("Watcher setup canceled");
                        Logger.Debug("Directory {0} watcher setup error", _folder);
                    }
                    IsInitialized = true;
                }, watcherTaskCancelationTokenSource.Token);
            _watcherTaskCancelationTokenSource = watcherTaskCancelationTokenSource;
            if (timeout > TimeSpan.Zero)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    try
                    {
                        if (!_watcherSetupTask.Wait(timeout))
                            CancelBeginWatch();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "CancelBeginWatch exception");
                    }
                });
            }
        }

        protected virtual void CancelBeginWatch()
        {
            var watcherTask = _watcherSetupTask;
            if (watcherTask != null && watcherTask.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                _watcherTaskCancelationTokenSource.Cancel();
                watcherTask.Wait();
                Debug.WriteLine($"MediaDirectory: BeginWatch for {Folder} canceled.", _folder);
                Logger.Debug("BeginWatch for {0} canceled.", _folder, null);
            }
        }

        protected virtual void FileRemoved(string fullPath)
        {
            foreach (var m in FindMediaList(m => fullPath.Equals(((MediaBase)m).FullPath, StringComparison.CurrentCultureIgnoreCase) && m.MediaStatus != TMediaStatus.Required))
            {
                MediaRemove(m);
                NotifyMediaDeleted(m);
            }
        }

        private void OnFileCreated(object source, FileSystemEventArgs e)
        {
            try
            {
                AddFile(e.FullPath);
            }
            catch
            {
                // ignored
            }
        }

        private void OnFileDeleted(object source, FileSystemEventArgs e)
        {
            try
            {
                FileRemoved(e.FullPath);
                GetVolumeInfo();
            }
            catch
            {
                // ignored
            }
        }

        protected virtual void OnFileRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                var m = FindMediaFirstByFullPath(e.OldFullPath);
                if (m == null)
                {
                    FileInfo fi = new FileInfo(e.FullPath);
                    AddFile(e.FullPath, fi.LastWriteTimeUtc);
                }
                else
                {
                    if (AcceptFile(e.FullPath))
                    {
                        OnMediaRenamed((MediaBase)m, e.FullPath);
                        ((MediaBase)m).FullPath = e.FullPath;
                    }
                    else
                        MediaRemove(m);
                }
            }
            catch
            {
                // ignored
            }
        }

        protected virtual void OnFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                var m = FindMediaFirstByFullPath(e.FullPath);
                if (m != null)
                    OnMediaChanged(m);
                GetVolumeInfo();
            }
            catch
            {
                // ignored
            }
        }

        protected virtual bool AcceptFile(string fullPath)
        {
            return true;
        }

        protected virtual IMedia AddFile(string fullPath, DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;
            MediaBase newMedia = FindMediaFirstByFullPath(fullPath);
            if (newMedia == null && AcceptFile(fullPath))
            {
                newMedia = (MediaBase)CreateMedia(fullPath, guid);
                newMedia.MediaName = Path.GetFileName(fullPath);
                newMedia.LastUpdated = lastWriteTime == default(DateTime) && File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                newMedia.MediaType = (FileUtils.StillFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Still : (FileUtils.VideoFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Movie : TMediaType.Unknown;
            }
            return newMedia;
        }

        protected virtual void OnError(object source, ErrorEventArgs e)
        {
            Debug.WriteLine("MediaDirectory: Watcher {0} returned error: {1}.", _folder, e.GetException());
            Logger.Warn("MediaDirectory: Watcher {0} returned error: {1} and will be restarted.", _folder, e.GetException());
            BeginWatch(_watcherFilter, _watcherIncludeSubdirectories, _watcherTimeout);
        }

        protected abstract IMedia CreateMedia(string fullPath, Guid guid = default(Guid));

        protected MediaBase FindMediaFirstByFullPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return null;
            lock (((IDictionary) Files).SyncRoot)
                return Files.Values.FirstOrDefault(f => fullPath.Equals(f.FullPath, StringComparison.CurrentCultureIgnoreCase));
        }

        public virtual void MediaAdd(MediaBase media)
        {
            MediaBase prevMedia;
            lock (((IDictionary) Files).SyncRoot)
            {
                if (Files.TryGetValue(media.MediaGuid, out prevMedia))
                {
                    if (prevMedia is IServerMedia || prevMedia is IAnimatedMedia)
                    {
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            (prevMedia as IAnimatedMedia)?.DbDeleteMedia();
                            (prevMedia as IServerMedia)?.DbDeleteMedia();
                            Logger.Warn("Media {0} replaced in dictionary. Previous media deleted in database.",
                                prevMedia);
                        });
                        Debug.WriteLine(prevMedia, "Media replaced in dictionary");
                    }
                }
                Files[media.MediaGuid] = media;
            }
            media.PropertyChanged += _media_PropertyChanged;
            MediaAdded?.Invoke(this, new MediaEventArgs(media));
        }

        public virtual void MediaRemove(IMedia media)
        {
            lock (((IDictionary) Files).SyncRoot)
                Files.Remove(media.MediaGuid);
            MediaRemoved?.Invoke(this, new MediaEventArgs(media));
            media.PropertyChanged -= _media_PropertyChanged;
            ((MediaBase)media).Dispose();
        }

        public string GetUniqueFileName(string fileName)
        {
            return FileUtils.GetUniqueFileName(_folder, fileName);
        }

        protected virtual void OnMediaRenamed(MediaBase media, string newFullPath)
        {
            Logger.Trace("Media {0} renamed: {1}", media, newFullPath);
        }

        protected virtual void OnMediaChanged(IMedia media)
        {
            if (media.IsVerified)
                media.ReVerify();
        }

        protected virtual void NotifyMediaDeleted(IMedia media)
        {
            MediaDeleted?.Invoke(this, new MediaEventArgs(media));
        }

        protected virtual void Reinitialize()
        {
            UnInitialize();
            Initialize();
        }

        protected virtual void ClearFiles()
        {
            lock (((IDictionary)Files).SyncRoot)
                Files.Values.ToList().ForEach(m => m.Remove());
        }

        protected virtual void GetVolumeInfo()
        {
            _volumeTotalSize = 0;
            ulong dummy;
            ulong free;
            ulong total;
            if (GetDiskFreeSpaceEx(Folder, out free, out total, out dummy))
            {
                VolumeFreeSize = (long)free;
                VolumeTotalSize = (long)total;
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            CancelBeginWatch();
            ClearFiles();
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void _media_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MediaPropertyChanged?.Invoke(this, new MediaPropertyChangedEventArgs(sender as IMedia, e.PropertyName));
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        
    }

}
