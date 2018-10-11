using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Newtonsoft.Json;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Server;

namespace TAS.Server.Media
{
    public abstract class MediaDirectoryBase : DtoBase, IMediaDirectory, IMediaDirectoryServerSide
    {
        private long _volumeFreeSize;
        private long _volumeTotalSize;
        private string _folder;
        protected Logger Logger;

        protected MediaDirectoryBase()
        {
            Logger = LogManager.GetLogger(GetType().Name);
        }

        [XmlIgnore]
        public IMediaManager MediaManager { get; set; }

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

        [JsonProperty]
        public string DirectoryName { get; set; }

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

        public abstract IMedia CreateMedia(IMediaProperties mediaProperties);

        public event EventHandler<MediaEventArgs> MediaAdded;
        public event EventHandler<MediaEventArgs> MediaRemoved;


        protected virtual void GetVolumeInfo()
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


    }

}