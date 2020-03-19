using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using jNet.RPC.Server;
using Newtonsoft.Json;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public abstract class MediaDirectoryBase : ServerObjectBase, IMediaDirectoryServerSide
    {
        private long _volumeFreeSize;
        private long _volumeTotalSize;
        private string _folder;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<MediaEventArgs> MediaVerified;
        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;

        internal event EventHandler<MediaPropertyChangedEventArgs> MediaPropertyChanged;


        [JsonProperty, XmlIgnore]
        public bool HaveFileWatcher { get; protected set; }

        [XmlIgnore, JsonProperty]
        public virtual long VolumeFreeSize
        {
            get => _volumeFreeSize;
            protected set => SetField(ref _volumeFreeSize, value);
        }

        [XmlIgnore, JsonProperty]
        public virtual long VolumeTotalSize
        {
            get => _volumeTotalSize;
            protected set => SetField(ref _volumeTotalSize, value);
        }

        [JsonProperty]
        public string Folder
        {
            get => _folder;
            set => SetField(ref _folder, value);
        }

        [JsonProperty]
        public virtual char PathSeparator => Path.DirectorySeparatorChar;

        public bool DirectoryExists()
        {
            return Directory.Exists(Folder);
        }

        public string GetUniqueFileName(string fileName)
        {
            return FileUtils.GetUniqueFileName(_folder, fileName);
        }

        public virtual bool FileExists(string filename, string subfolder = null)
        {
            return File.Exists(Path.Combine(Folder, subfolder ?? string.Empty, filename));
        }

        public virtual void AddMedia(IMedia media)
        {
            if (!(media is MediaBase mediaBase))
                return;
            mediaBase.Directory = this;
            MediaAdded?.Invoke(this, new MediaEventArgs(media));
        }

        public virtual void RemoveMedia(IMedia media)
        {
            MediaRemoved?.Invoke(this, new MediaEventArgs(media));
        }

        public abstract IMediaSearchProvider Search(TMediaCategory? category, string searchString);

        internal abstract IMedia CreateMedia(IMediaProperties media);
        
        internal virtual bool DeleteMedia(IMedia media)
        {
            if (media.FileExists())
            {
                try
                {
                    File.Delete(((MediaBase)media).FullPath);
                    Logger.Trace("File deleted {0}", media);
                    RemoveMedia(media);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
            else
            {
                RemoveMedia(media);
                return true;
            }
            return false;
        }
        
        internal virtual void RefreshVolumeInfo()
        {
            if (GetDiskFreeSpaceEx(Folder, out var free, out var total, out var dummy))
            {
                VolumeFreeSize = (long)free;
                VolumeTotalSize = (long)total;
            }
            else
            {
                VolumeFreeSize = 0;
                VolumeTotalSize = 0;
            }
        }
        

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);


        internal void NotifyMediaVerified(IMedia mediaBase)
        {
            MediaVerified?.Invoke(this, new MediaEventArgs(mediaBase));
        }

        protected void NotifyMediaPropertyChanged(IMedia media, PropertyChangedEventArgs eventArgs)
        {
            MediaPropertyChanged?.Invoke(this, new MediaPropertyChangedEventArgs(media, eventArgs.PropertyName));
        }
    }

}