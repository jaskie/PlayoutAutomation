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

namespace TAS.Server
{
    public enum TDirectoryAccessType { Direct, FTP };
    public abstract class MediaDirectory : IDisposable, INotifyPropertyChanged
    {
        public readonly static string[] VideoFileTypes = { ".mov", ".mxf", ".mkv", ".mp4", ".wmv", ".avi", ".lxf" };
        public readonly static string[] StillFileTypes = { ".tif", ".tga", ".png", ".tiff", ".jpg", ".gif", ".bmp" };
        
        public static string DefaultFileExtension(TMediaType type)
        {
            if (type == TMediaType.Movie || type == TMediaType.Unknown)
                return VideoFileTypes[0];
            if (type == TMediaType.Still)
                return StillFileTypes[0];
            throw new NotImplementedException(string.Format("MediaDirectory:DefaultFileExtension {0}", type));
        }

        protected string _folder;
        protected string[] _extensions;
        private FileSystemWatcher _watcher;
        protected ConcurrentHashSet<Media> _files = new ConcurrentHashSet<Media>();

        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;
        public event EventHandler<MediaEventArgs> MediaVerified;

        protected bool _isInitialized = false;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public virtual void Initialize()
        {
            if (!_isInitialized)
            {
                BeginWatch(null, true);
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
            _files.ToList().ForEach(m => m.Remove());
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out UInt64 lpFreeBytesAvailable, out UInt64 lpTotalNumberOfBytes, out UInt64 lpTotalNumberOfFreeBytes);

        protected virtual void GetVolumeInfo()
        {
            _volumeTotalSize = 0;
            if (AccessType == TDirectoryAccessType.Direct)
            {
                UInt64 dummy = 0;
                if (GetDiskFreeSpaceEx(Folder, out _volumeFreeSize, out _volumeTotalSize, out dummy))
                    NotifyPropertyChanged("VolumeFreeSize");
            }
        }

        private UInt64 _volumeFreeSize = 0;
        
        [XmlIgnore]
        public virtual UInt64 VolumeFreeSize
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

        private UInt64 _volumeTotalSize = 0;
        public virtual UInt64 VolumeTotalSize { get { return _volumeTotalSize; } }

        public abstract void Refresh();
        
        [XmlIgnore]
        public virtual List<Media> Files
        {
            get
            {
                _files.Lock.EnterUpgradeableReadLock();
                try
                {
                    return _files.ToList();
                }
                finally
                {
                    _files.Lock.ExitUpgradeableReadLock();
                }
            }
        }

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

        public string DirectoryName { get; set; }

        [XmlIgnore]
        public TDirectoryAccessType AccessType { get; protected set; }

        public string Username { get; set; }

        public string Password { get; set; }

        private NetworkCredential _networkCredential;
        internal NetworkCredential NetworkCredential
        {
            get
            {
                if (_networkCredential == null)
                    _networkCredential = new NetworkCredential(Username, Password);
                return _networkCredential;
            }
        }
        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get { return _extensions; } set { _extensions = value; } }
        
        public virtual bool FileExists(string filename, string subfolder = null)
        {
            return File.Exists(Path.Combine(_folder, subfolder ?? string.Empty, filename));
        }

        public virtual bool DeleteMedia(Media media)
        {
            if (media.Directory == this)
            {
                bool isLastWithTheName = false;
                _files.Lock.EnterReadLock();
                try
                {
                    isLastWithTheName = !_files.Any(m => m.FullPath == media.FullPath && m != media);
                }
                finally
                {
                    _files.Lock.ExitReadLock();
                }
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
                    return true;
                }
            }
            return false;
        }

        protected abstract Media CreateMedia(string fileNameOnly);

        public abstract void SweepStaleMedia();

        protected virtual Media AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime))
        {
            if (_extensions == null
                || _extensions.Count() == 0
                || _extensions.Any(ext => ext.ToLowerInvariant() == Path.GetExtension(fullPath).ToLowerInvariant()))
            {
                Media newMedia;
                _files.Lock.EnterReadLock();
                try
                {
                    newMedia = _files.FirstOrDefault(m => fullPath.Equals(m.FullPath));
                }
                finally
                {
                    _files.Lock.ExitReadLock();
                }
                if (newMedia == null)
                {
                    newMedia = CreateMedia(Path.GetFileName(fullPath));
                    newMedia._mediaName = (_extensions == null || _extensions.Length == 0) ? Path.GetFileName(fullPath) : Path.GetFileNameWithoutExtension(fullPath);
                    newMedia.LastUpdated = lastWriteTime == default(DateTime) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                    newMedia.MediaType = (StillFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Still : (VideoFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Movie : TMediaType.Unknown;
                }
                return newMedia;
            }
            return null;
        }

        public virtual void MediaAdd(Media media)
        {
            _files.Add(media);
            var h = MediaAdded;
            if (h != null)
                h(this, new MediaEventArgs(media));
        }

        public virtual void MediaRemove(Media media)
        {
            if (_files.Remove(media))
            {
                var h = MediaRemoved;
                if (h != null)
                    h(this, new MediaEventArgs(media));
            }
        }
        
        protected virtual void FileRemoved(string fullPath)
        {
            _files.Lock.EnterUpgradeableReadLock();
            try
            {
                foreach (Media m in _files.Where(m => fullPath == m.FullPath && m.MediaStatus != TMediaStatus.Required).ToList())
                    MediaRemove(m);
            }
            finally
            {
                _files.Lock.ExitUpgradeableReadLock();
            }
        }

        protected virtual void OnMediaRenamed(Media media, string newName)
        {
            media.Renamed(newName);
        }

        protected virtual void OnMediaChanged(Media media)
        {
            if (media.Verified)
            {
                media.Verified = false;
                media.MediaStatus = TMediaStatus.Unknown;
                ThreadPool.QueueUserWorkItem(o => media.Verify());
            }
        }

        internal virtual void OnMediaVerified(Media media)
        {
            var h = MediaVerified;
            if (h != null)
                h(media, new MediaEventArgs(media));
        }

        protected virtual void EnumerateFiles(string filter, CancellationToken cancelationToken)
        {
            IEnumerable<FileSystemInfo> list = (new DirectoryInfo(_folder)).EnumerateFiles(string.IsNullOrWhiteSpace(filter)? "*": string.Format("*{0}*", filter));
            foreach (FileSystemInfo f in list)
            {
                _files.Lock.EnterUpgradeableReadLock();
                try
                {
                    if (cancelationToken.IsCancellationRequested)
                        return;
                    AddFile(f.FullName, f.CreationTimeUtc, f.LastWriteTimeUtc);
                }
                finally
                {
                    _files.Lock.ExitUpgradeableReadLock();
                }
            }
        }

        public bool Exists { get { return Directory.Exists(_folder); } }

        public virtual Media FindMedia(Media media)
        {
            _files.Lock.EnterReadLock();
            try
            {
                return _files.FirstOrDefault(m => m.Equals(media));
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

        public virtual Media FindMedia(Guid mediaGuid)
        {
            
            _files.Lock.EnterReadLock();
            try
            {
                return _files.FirstOrDefault(m => m.MediaGuid == mediaGuid);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

        public virtual List<Media> FindMedia(Func<Media, bool> condition)
        {
            _files.Lock.EnterReadLock();
            try
            {
                return _files.Where(condition).ToList();
             }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

        protected string _filter;
        protected CancellationTokenSource _watcherTaskCancelationTokenSource;
        protected System.Threading.Tasks.Task _watcherTask;
        protected void BeginWatch(string filter, bool setIsInitalized)
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
                    _filter = filter;
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
                                EnumerateFiles(filter, watcherCancelationToken);
                                _watcher = new FileSystemWatcher(_folder)
                                {
                                    Filter = string.IsNullOrWhiteSpace(filter) ? string.Empty : string.Format("*{0}*", filter),
                                    IncludeSubdirectories = false,
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
                        catch { };
                        if (!watcherReady)
                            System.Threading.Thread.Sleep(30000); //Wait for retry 30 sec.
                    }
                    Debug.WriteLine("MediaDirectory: Watcher {0} setup successful.", (object)_folder);
                    if (setIsInitalized)
                        IsInitialized = true;
                }, watcherCancelationToken);
        }

        protected virtual void CancelBeginWatch()
        {
            var watcherTask = _watcherTask;
            if (watcherTask != null && watcherTask.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                _watcherTaskCancelationTokenSource.Cancel();
                watcherTask.Wait();
            }
        }



        private void OnFileCreated(object source, FileSystemEventArgs e)
        {
            AddFile(e.FullPath);
        }

        private void OnFileDeleted(object source, FileSystemEventArgs e)
        {
            FileRemoved(e.FullPath);
            GetVolumeInfo();
        }

        protected virtual void OnFileRenamed(object source, RenamedEventArgs e)
        {
            Media m;
            _files.Lock.EnterReadLock();
            try
            {
                m = _files.FirstOrDefault(f => e.OldFullPath == f.FullPath);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            if (m != null)
                OnMediaRenamed(m, e.Name);
        }

        protected virtual void OnFileChanged(object source, FileSystemEventArgs e)
        {
            Media m;
            _files.Lock.EnterReadLock();
            try
            {
                m = _files.FirstOrDefault(f => e.FullPath == f.FullPath);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            if (m != null)
                OnMediaChanged(m);
            GetVolumeInfo();
        }

        protected virtual void OnError(object source, ErrorEventArgs e)
        {
            Debug.WriteLine("MediaDirectory: Watcher {0} returned error: {1}.", _folder, e.GetException());
            BeginWatch(_filter, false);
        }

        public override string ToString()
        {
            return DirectoryName + " (" + _folder + ")";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
