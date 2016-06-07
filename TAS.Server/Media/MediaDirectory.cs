using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TAS.Remoting.Server;

namespace TAS.Server
{
    public abstract class MediaDirectory : DtoBase, IMediaDirectory
    { 
        private FileSystemWatcher _watcher;
        protected ConcurrentDictionary<Guid, IMedia> _files = new ConcurrentDictionary<Guid, IMedia>();
        internal MediaManager MediaManager;

        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;
        public event EventHandler<MediaEventArgs> MediaVerified;
        public event EventHandler<MediaEventArgs> MediaDeleted;

        protected bool _isInitialized = false;

        public MediaDirectory(MediaManager mediaManager)
        {
            MediaManager = mediaManager;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Initialize()
        {
            if (!_isInitialized)
            {
                BeginWatch("*", false, TimeSpan.Zero); 
            }
        }

        public virtual void UnInitialize()
        {
            if (_isInitialized)
            {
                CancelBeginWatch();
                ClearFiles();
                IsInitialized = false;
            }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                DoDispose();
        }

        protected virtual void DoDispose()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        protected virtual void Reinitialize()
        {
            if (_isInitialized)
            {
                UnInitialize();
                Initialize();
            }
        }

        protected virtual void ClearFiles()
        {
            _files.Values.ToList().ForEach(m => ((Media)m).Remove());
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

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

        public bool DirectoryExists()
        {
            return Directory.Exists(Folder);
        }

        private long _volumeFreeSize = 0;
        
        [XmlIgnore]
        [JsonProperty]
        public virtual long VolumeFreeSize
        {
            get { return _volumeFreeSize; }
            protected set
            {
                if (_volumeFreeSize != value)
                {
                    _volumeFreeSize = value;
                    NotifyPropertyChanged("VolumeFreeSize");
                }
            }
        }

        private long _volumeTotalSize = 0;
        [XmlIgnore]
        [JsonProperty]
        public virtual long VolumeTotalSize
        {
            get { return _volumeTotalSize; }
            protected set
            {
                if (_volumeTotalSize != value)
                {
                    _volumeTotalSize = value;
                    NotifyPropertyChanged("VolumeTotalSize");
                }
            }
        }
        public abstract void Refresh();

        public virtual ICollection<IMedia> GetFiles()
        {
            return _files.Values.ToList();
        }

        protected string _folder;
        [JsonProperty]
        public string Folder
        {
            get { return _folder; }
            set
            {
                if (value != _folder)
                {
                    _folder = value;
                    Reinitialize();
                }
            }
        }

        [JsonProperty]
        public virtual char PathSeparator { get { return Path.DirectorySeparatorChar; } }

        [XmlIgnore]
        public bool IsInitialized
        {
            get { return _isInitialized; }
            protected set {
                if (value != _isInitialized)
                {
                    _isInitialized = value;
                    NotifyPropertyChanged("IsInitialized");
                }
            }
        }

        [JsonProperty]
        public string DirectoryName { get; set; }

        
        public virtual bool FileExists(string filename, string subfolder = null)
        {
            return File.Exists(Path.Combine(_folder, subfolder ?? string.Empty, filename));
        }

        public virtual bool DeleteMedia(IMedia media)
        {
            if (media.Directory == this)
            {
                bool isLastWithTheName = false;
                    isLastWithTheName = !_files.Values.Any(m => m.FullPath == media.FullPath && m != media);
                if (isLastWithTheName && media.FileExists())
                {
                    try
                    {
                        File.Delete(media.FullPath);
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
                    OnMediaDeleted(media);
                    return true;
                }
            }
            return false;
        }

        protected abstract IMedia CreateMedia(string fullPath, Guid guid = default(Guid));

        public abstract void SweepStaleMedia();

        protected virtual bool AcceptFile(string fullPath)
        {
            return true;
        }

        protected virtual IMedia AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            Media newMedia;
            newMedia = (Media)_files.Values.FirstOrDefault(m => fullPath.Equals(m.FullPath));
            if (newMedia == null && AcceptFile(fullPath))
            {
                newMedia = (Media)CreateMedia(fullPath, guid);
                newMedia.MediaName = Path.GetFileName(fullPath);
                newMedia.LastUpdated = lastWriteTime == default(DateTime) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                newMedia.MediaType = (FileUtils.StillFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Still : (FileUtils.VideoFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Movie : TMediaType.Unknown;
            }
            return newMedia;
        }

        public virtual void MediaAdd(IMedia media)
        {
            _files[media.MediaGuid] = media;
            NotifyMediaAdded(media);
        }

        public virtual void MediaRemove(IMedia media)
        {
            IMedia removed;
            _files.TryRemove(media.MediaGuid, out removed);
            var h = MediaRemoved;
            if (h != null)
                h(this, new MediaEventArgs(media));
        }

        protected virtual void FileRemoved(string fullPath)
        {
            foreach (Media m in _files.Values.Where(m => fullPath == m.FullPath && m.MediaStatus != TMediaStatus.Required).ToList())
            {
                MediaRemove(m);
                OnMediaDeleted(m);
            }
        }

        protected virtual void OnMediaRenamed(Media media, string newName) { }

        protected virtual void OnMediaChanged(IMedia media)
        {
            if (media.Verified)
            {
                media.ReVerify();
            }
        }

        public virtual void OnMediaVerified(IMedia media)
        {
            var h = MediaVerified;
            if (h != null)
                h(this, new MediaEventArgs(media));
        }

        protected virtual void OnMediaDeleted(IMedia media)
        {
            var h = MediaDeleted;
            if (h != null)
                h(this, new MediaEventArgs(media));
        }


        protected virtual void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            IEnumerable<FileSystemInfo> list = (new DirectoryInfo(directory)).EnumerateFiles(string.IsNullOrWhiteSpace(filter) ? "*" : string.Format("*{0}*", filter));
            foreach (FileSystemInfo f in list)
            {
                if (cancelationToken.IsCancellationRequested)
                    return;
                AddFile(f.FullName, f.CreationTimeUtc, f.LastWriteTimeUtc);
            }
            if (includeSubdirectories)
            {
                list = (new DirectoryInfo(directory)).EnumerateDirectories();
                foreach (FileSystemInfo d in list)
                    EnumerateFiles(d.FullName, filter, includeSubdirectories, cancelationToken);
            }
        }

        public bool Exists { get { return Directory.Exists(_folder); } }

        public virtual IMedia FindMediaByMediaGuid(Guid mediaGuid)
        {
            IMedia result;
            _files.TryGetValue(mediaGuid, out result);
            return result;
        }

        public virtual List<IMedia> FindMediaList(Func<IMedia, bool> condition)
        {
            return _files.Values.Where(condition).ToList();
        }

        public virtual IMedia FindMediaFirst(Func<IMedia, bool> condition)
        {
            return _files.Values.FirstOrDefault(condition);
        }


        private string _watcherFilter;
        private TimeSpan _watcherTimeout;
        private bool _watcherIncludeSubdirectories;
        protected CancellationTokenSource _watcherTaskCancelationTokenSource;
        protected System.Threading.Tasks.Task _watcherTask;
        protected void BeginWatch(string filter, bool includeSubdirectories, TimeSpan timeout)
        {
            var oldTask = _watcherTask;
            if (oldTask != null && oldTask.Status == System.Threading.Tasks.TaskStatus.Running)
                return;
            var watcherTaskCancelationTokenSource = new CancellationTokenSource();
            var watcherCancelationToken = watcherTaskCancelationTokenSource.Token;
            _watcherTaskCancelationTokenSource = watcherTaskCancelationTokenSource;
            _watcherTask = System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                {
                    _watcherFilter = filter;
                    _watcherTimeout = timeout;
                    _watcherIncludeSubdirectories = includeSubdirectories;
                    bool watcherReady = false;
                    while (!watcherReady)
                    {
                        if (watcherCancelationToken.IsCancellationRequested)
                        {
                            Debug.WriteLine("Watcher setup canceled");
                            return;
                        }
                        try
                        {
                            GetVolumeInfo();
                            if (_watcher != null)
                            {
                                _watcher.Dispose();
                                _watcher = null;
                            }
                            if (Directory.Exists(_folder))
                            {
                                EnumerateFiles(_folder, filter, includeSubdirectories, watcherCancelationToken);
                                _watcher = new FileSystemWatcher(_folder)
                                {
                                    Filter = string.IsNullOrWhiteSpace(filter) ? string.Empty : string.Format("*{0}*", filter),
                                    IncludeSubdirectories = includeSubdirectories,
                                    EnableRaisingEvents = true,
                                };
                                _watcher.Created += OnFileCreated;
                                _watcher.Deleted += OnFileDeleted;
                                _watcher.Renamed += OnFileRenamed;
                                _watcher.Changed += OnFileChanged;
                                _watcher.Error += OnError;
                                watcherReady = _watcher.EnableRaisingEvents;
                            }
                        }
                        catch { };
                        if (!watcherReady)
                            System.Threading.Thread.Sleep(30000); //Wait for retry 30 sec.
                    }
                    Debug.WriteLine("MediaDirectory: Watcher {0} setup successful.", (object)_folder);
                    IsInitialized = true;
                }, watcherCancelationToken);
            if (timeout != TimeSpan.Zero)
            {
                ThreadPool.QueueUserWorkItem(o => {
                if (!_watcherTask.Wait(timeout))
                    CancelBeginWatch();
                });
            }
        }

        protected virtual void CancelBeginWatch()
        {
            var watcherTask = _watcherTask;
            if (watcherTask != null && watcherTask.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                _watcherTaskCancelationTokenSource.Cancel();
                watcherTask.Wait();
                Debug.WriteLine("MediaDirectory: BeginWatch for {0} canceled.", (object)_folder);
            }
        }



        private void OnFileCreated(object source, FileSystemEventArgs e)
        {
            try
            {
                AddFile(e.FullPath);
            }
            catch { }
        }

        private void OnFileDeleted(object source, FileSystemEventArgs e)
        {
            try
            {
                FileRemoved(e.FullPath);
                GetVolumeInfo();
            }
            catch { }
        }

        protected virtual void OnFileRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                Media m = (Media)_files.Values.FirstOrDefault(f => e.OldFullPath == f.FullPath);
                if (m == null)
                {
                    FileInfo fi = new FileInfo(e.FullPath);
                    AddFile(e.FullPath, fi.CreationTimeUtc, fi.LastWriteTimeUtc);
                }
                else
                {
                    m.FullPath = e.FullPath;
                    OnMediaRenamed(m, e.Name);
                }
            }
            catch { }
        }

        protected virtual void OnFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                IMedia m = _files.Values.FirstOrDefault(f => e.FullPath == f.FullPath);
                if (m != null)
                    OnMediaChanged(m);
                GetVolumeInfo();
            }
            catch { }
        }

        protected virtual void OnError(object source, ErrorEventArgs e)
        {
            Debug.WriteLine("MediaDirectory: Watcher {0} returned error: {1}.", _folder, e.GetException());
            BeginWatch(_watcherFilter, _watcherIncludeSubdirectories, _watcherTimeout);
        }

        public override string ToString()
        {
            return string.Format("{0}:{1} ({2})", this.GetType().Name, DirectoryName, Folder);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        internal virtual void NotifyMediaAdded (IMedia media)
        {
            var h = MediaAdded;
            if (h != null)
                h(this, new MediaEventArgs(media));
        }
    }

}
