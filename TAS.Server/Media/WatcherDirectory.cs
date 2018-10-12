using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    [DebuggerDisplay("{DirectoryName} ({_folder})")]
    public abstract class WatcherDirectory : MediaDirectoryBase, IWatcherDirectory
    { 
        private FileSystemWatcher _watcher;
        private bool _isInitialized;
        private string _watcherFilter;
        private TimeSpan _watcherTimeout;
        private bool _watcherIncludeSubdirectories;
        private CancellationTokenSource _watcherTaskCancelationTokenSource;
        private Task _watcherSetupTask;

        protected readonly Dictionary<Guid, MediaBase> Files = new Dictionary<Guid, MediaBase>();

        public virtual void Refresh()
        {
            UnInitialize();
            Initialize();
        }

        public event EventHandler<MediaEventArgs> MediaVerified;
        public event EventHandler<MediaEventArgs> MediaDeleted;
        internal event EventHandler<MediaPropertyChangedEventArgs> MediaPropertyChanged;
        
        protected WatcherDirectory(MediaManager mediaManager)
        {
            MediaManager = mediaManager;
            Logger = NLog.LogManager.GetLogger(GetType().Name);
        }

#if DEBUG
        ~WatcherDirectory()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        [XmlIgnore, JsonProperty]
        public bool IsInitialized
        {
            get => _isInitialized;
            protected set => SetField(ref _isInitialized, value);
        }


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

        public virtual IEnumerable<IMedia> GetFiles()
        {
            lock (((IDictionary)Files).SyncRoot)
                return Files.Values.Cast<IMedia>().ToList().AsReadOnly();
        }
        
        public virtual bool DeleteMedia(IMedia media)
        {
            if (media.Directory != this) 
                throw new ApplicationException("Deleting media directory is invalid");
            var fullPath = ((MediaBase)media).FullPath;
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
                RemoveMedia(media);
                NotifyMediaDeleted(media);
                return true;
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

        internal void UpdateMediaGuid(MediaBase media, Guid newGuid)
        {
            if (newGuid == media.MediaGuid)
                return;
            lock (((IDictionary) Files).SyncRoot)
            {
                if (Files.ContainsKey(media.MediaGuid))
                    Files.Remove(media.MediaGuid);
                media.MediaGuid = newGuid;
                Files.Add(newGuid, media);
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
                var m = FindMediaFirstByFullPath(f.FullName);
                if (m != null)
                    continue;
                AddMediaFromPath(f.FullName, f.LastWriteTimeUtc);
            }
            if (!includeSubdirectories) return;
            list = new DirectoryInfo(directory).EnumerateDirectories();
            foreach (var d in list)
                EnumerateFiles(d.FullName, filter, true, cancelationToken);
        }

        protected void BeginWatch(string filter, bool includeSubdirectories, TimeSpan timeout)
        {
            var oldTask = _watcherSetupTask;
            if (oldTask != null && oldTask.Status != TaskStatus.RanToCompletion)
                return;
            var watcherTaskCancelationTokenSource = new CancellationTokenSource();
            _watcherSetupTask = Task.Factory.StartNew(
                () =>
                {
                    _watcherFilter = filter;
                    _watcherTimeout = timeout;
                    _watcherIncludeSubdirectories = includeSubdirectories;
                    var watcherReady = false;
                    while (!watcherReady && !watcherTaskCancelationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            if (_watcher != null)
                            {
                                _watcher.Dispose();
                                _watcher = null;
                            }
                            if (Directory.Exists(Folder))
                            {
                                GetVolumeInfo();
                                EnumerateFiles(Folder, filter, includeSubdirectories, watcherTaskCancelationTokenSource.Token);
                                _watcher = new FileSystemWatcher(Folder)
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
                            Logger.Error(e, "Directory {0} watcher setup error", Folder);
                        }
                        if (!watcherReady)
                            Thread.Sleep(30000); //Wait for retry 30 sec.
                    }
                    if (watcherReady)
                        Debug.WriteLine("MediaDirectory: Watcher {0} setup successful.", (object)Folder);
                    else
                    if (watcherTaskCancelationTokenSource.IsCancellationRequested)
                    {
                        Debug.WriteLine("Watcher setup canceled");
                        Logger.Debug("Directory {0} watcher setup error", Folder);
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
            if (watcherTask != null && watcherTask.Status == TaskStatus.Running)
            {
                _watcherTaskCancelationTokenSource.Cancel();
                watcherTask.Wait();
                Debug.WriteLine($"MediaDirectory: BeginWatch for {Folder} canceled.");
                Logger.Debug("BeginWatch for {0} canceled.", Folder, null);
            }
        }

        protected virtual void FileRemoved(string fullPath)
        {
            foreach (var m in FindMediaList(m => fullPath.Equals(((MediaBase)m).FullPath, StringComparison.CurrentCultureIgnoreCase) && m.MediaStatus != TMediaStatus.Required))
            {
                RemoveMedia(m);
                NotifyMediaDeleted(m);
            }
        }

        private void OnFileCreated(object source, FileSystemEventArgs e)
        {
            try
            {
                AddMediaFromPath(e.FullPath, File.GetLastWriteTimeUtc(e.FullPath));
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
                    var fi = new FileInfo(e.FullPath);
                    AddMediaFromPath(e.FullPath, fi.LastWriteTimeUtc);
                }
                else
                {
                    if (AcceptFile(e.FullPath))
                    {
                        OnMediaRenamed(m, e.FullPath);
                        m.FileName = e.Name;
                    }
                    else
                        RemoveMedia(m);
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

        protected virtual void OnError(object source, ErrorEventArgs e)
        {
            Debug.WriteLine("MediaDirectory: Watcher {0} returned error: {1}.", Folder, e.GetException());
            Logger.Warn("MediaDirectory: Watcher {0} returned error: {1} and will be restarted.", Folder, e.GetException());
            BeginWatch(_watcherFilter, _watcherIncludeSubdirectories, _watcherTimeout);
        }

        protected MediaBase FindMediaFirstByFullPath(string fullPath)
        {
            lock (((IDictionary) Files).SyncRoot)
                return Files.Values.FirstOrDefault(f => fullPath.Equals(f.FullPath, StringComparison.CurrentCultureIgnoreCase));
        }

        public override void AddMedia(IMedia media)
        {
            if (!(media is MediaBase mediaBase))
                throw new ApplicationException("Invalid type provided to AddMedia");
            base.AddMedia(media);
            mediaBase.Directory = this;
            mediaBase.PropertyChanged += _media_PropertyChanged;
            lock (((IDictionary) Files).SyncRoot)
            {
                if (Files.TryGetValue(mediaBase.MediaGuid, out var prevMedia))
                {
                    if (prevMedia is IServerMedia || prevMedia is IAnimatedMedia)
                    {
                        prevMedia.PropertyChanged -= _media_PropertyChanged;
                        Task.Run(() =>
                        {
                            if (prevMedia is IAnimatedMedia am)
                                EngineController.Database.DeleteMedia(am);
                            if (prevMedia is IServerMedia sm)
                                EngineController.Database.DeleteMedia(sm);
                            Logger.Warn("Media {0} replaced in dictionary. Previous media deleted in database.",
                                prevMedia);
                            Debug.WriteLine(prevMedia, "Media replaced in dictionary");
                        });
                    }
                }
                Files[mediaBase.MediaGuid] = mediaBase;
            }
        }

        public override void RemoveMedia(IMedia media)
        {
            lock (((IDictionary) Files).SyncRoot)
                Files.Remove(media.MediaGuid);
            base.RemoveMedia(media);
            media.PropertyChanged -= _media_PropertyChanged;
            ((MediaBase)media).Dispose();
        }

        protected virtual void OnMediaRenamed(MediaBase media, string newFullPath)
        {
            Logger.Trace("Media {0} renamed: {1}", media, newFullPath);
        }

        protected virtual void OnMediaChanged(IMedia media)
        {
            if (!media.IsVerified)
                return;
            media.ReVerify();
            Logger.Trace("Media {0} changed", media);
        }

        protected virtual void NotifyMediaDeleted(IMedia media)
        {
            MediaDeleted?.Invoke(this, new MediaEventArgs(media));
        }

        protected virtual void ClearFiles()
        {
            lock (((IDictionary)Files).SyncRoot)
                Files.Values.ToList().ForEach(m => m.Remove());
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            CancelBeginWatch();
            ClearFiles();
            if (_watcher == null)
                return;
            _watcher.Dispose();
            _watcher = null;
        }

        private void _media_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MediaPropertyChanged?.Invoke(this, new MediaPropertyChangedEventArgs(sender as IMedia, e.PropertyName));
        }

        protected abstract IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated);
    }

}
