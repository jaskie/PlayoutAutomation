using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Newtonsoft.Json;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Server;

namespace TAS.Server.Media
{
    public abstract class MediaDirectoryBase : DtoBase, IMediaDirectory, IMediaDirectoryServerSide
    {
        private long _volumeFreeSize;
        private long _volumeTotalSize;
        private string _folder;
        protected Logger Logger;
        internal MediaManager MediaManager;

        protected MediaDirectoryBase()
        {
            Logger = LogManager.GetLogger(GetType().Name);
        }

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

        public virtual void AddMedia(IMedia media)
        {
            if (!(media is MediaBase mediaBase))
                return;
            mediaBase.Directory = this;
        }

        public abstract void RemoveMedia(IMedia media);

        public abstract IMedia CreateMedia(IMediaProperties mediaProperties);
        
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