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

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class MediaDirectory : IMediaDirectory, IDto
    { 
        protected string[] _extensions;
        private FileSystemWatcher _watcher;
        protected ConcurrentDictionary<Guid, IMedia> _files = new ConcurrentDictionary<Guid, IMedia>();
        internal MediaManager MediaManager;

        public event EventHandler<MediaDtoEventArgs> MediaAdded;
        public event EventHandler<MediaDtoEventArgs> MediaRemoved;
        public event EventHandler<MediaDtoEventArgs> MediaVerified;

        protected bool _isInitialized = false;
        private readonly Guid _idDto = Guid.NewGuid();

        public MediaDirectory(MediaManager mediaManager)
        {
            MediaManager = mediaManager;
        }

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

        [JsonProperty]
        public Guid DtoGuid { get { return _idDto; } }

        protected virtual void ClearFiles()
        {
            _files.Values.ToList().ForEach(m => ((Media)m).Remove());
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
        [XmlIgnore]
        public virtual UInt64 VolumeTotalSize { get { return _volumeTotalSize; } }

        public abstract void Refresh();

        public virtual IEnumerable<IMedia> GetFiles()
        {
            return _files.Values;
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

        [XmlIgnore]
        [JsonProperty]
        public TDirectoryAccessType AccessType { get; protected set; }

        public string Username { get; set; }

        public string Password { get; set; }

        private NetworkCredential _networkCredential;
        public NetworkCredential NetworkCredential
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
                    return true;
                }
            }
            return false;
        }

        protected abstract IMedia CreateMedia(string fileNameOnly);
        protected abstract IMedia CreateMedia(string fileNameOnly, Guid guid);

        public abstract void SweepStaleMedia();

        protected virtual IMedia AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            if (_extensions == null
                || _extensions.Count() == 0
                || _extensions.Any(ext => ext.ToLowerInvariant() == Path.GetExtension(fullPath).ToLowerInvariant()))
            {
                Media newMedia;
                    newMedia = (Media)_files.Values.FirstOrDefault(m => fullPath.Equals(m.FullPath));
                if (newMedia == null)
                {
                    if (guid == default(Guid))
                        newMedia = (Media)CreateMedia(Path.GetFileName(fullPath));
                    else
                        newMedia = (Media)CreateMedia(Path.GetFileName(fullPath), guid);
                    newMedia.MediaName = (_extensions == null || _extensions.Length == 0) ? Path.GetFileName(fullPath) : Path.GetFileNameWithoutExtension(fullPath);
                    newMedia.LastUpdated = lastWriteTime == default(DateTime) ? File.GetLastWriteTimeUtc(fullPath) : lastWriteTime;
                    newMedia.MediaType = (FileUtils.StillFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Still : (FileUtils.VideoFileTypes.Any(ve => ve == Path.GetExtension(fullPath).ToLowerInvariant())) ? TMediaType.Movie : TMediaType.Unknown;
                    NotifyMediaAdded(newMedia);
                }
                return newMedia;
            }
            return null;
        }

        public virtual void MediaAdd(IMedia media)
        {
            _files[media.DtoGuid] = media;
        }

        public virtual void MediaRemove(IMedia media)
        {
            IMedia removed;
            if (_files.TryRemove(media.DtoGuid, out removed))
            {
                var h = MediaRemoved;
                if (h != null)
                    h(this, new MediaDtoEventArgs(media.DtoGuid, media.MediaGuid));
            }
        }

        protected virtual void FileRemoved(string fullPath)
        {
            foreach (Media m in _files.Values.Where(m => fullPath == m.FullPath && m.MediaStatus != TMediaStatus.Required).ToList())
                MediaRemove(m);
        }

        protected virtual void OnMediaRenamed(IMedia media, string newName)
        {
            media.FileName = newName;
        }

        protected virtual void OnMediaChanged(IMedia media)
        {
            if (media.Verified)
            {
                ((Media)media).Verified = false;
                media.MediaStatus = TMediaStatus.Unknown;
                ThreadPool.QueueUserWorkItem(o => media.Verify());
            }
        }

        public virtual void OnMediaVerified(IMedia media)
        {
            var h = MediaVerified;
            if (h != null)
                h(media, new MediaDtoEventArgs(media.DtoGuid, media.MediaGuid));
        }

        protected virtual void EnumerateFiles(string filter, CancellationToken cancelationToken)
        {
            IEnumerable<FileSystemInfo> list = (new DirectoryInfo(_folder)).EnumerateFiles(string.IsNullOrWhiteSpace(filter) ? "*" : string.Format("*{0}*", filter));
            foreach (FileSystemInfo f in list)
            {
                if (cancelationToken.IsCancellationRequested)
                    return;
                AddFile(f.FullName, f.CreationTimeUtc, f.LastWriteTimeUtc);
            }
        }

        public bool Exists { get { return Directory.Exists(_folder); } }

        public virtual IMedia FindMediaByMediaGuid(Guid mediaGuid)
        {
            return _files.Values.FirstOrDefault(m => m.MediaGuid == mediaGuid);
        }

        public virtual IMedia FindMediaByDto(Guid guidDto)
        {
            IMedia result;
            _files.TryGetValue(guidDto, out result);
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
            IMedia m = _files.Values.FirstOrDefault(f => e.OldFullPath == f.FullPath);
            if (m != null)
                OnMediaRenamed(m, e.Name);
        }

        protected virtual void OnFileChanged(object source, FileSystemEventArgs e)
        {
            IMedia m = _files.Values.FirstOrDefault(f => e.FullPath == f.FullPath);
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
            return string.Format("{0} ({1})", DirectoryName, Folder);
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
                h(this, new MediaDtoEventArgs(media.DtoGuid, media.MediaGuid));
        }
    }

}
