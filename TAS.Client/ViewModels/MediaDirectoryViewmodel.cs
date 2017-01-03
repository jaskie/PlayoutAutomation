using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaDirectoryViewmodel
    {
        readonly IMediaDirectory _directory;
        readonly List<MediaDirectoryViewmodel> _subdirectories;

        public MediaDirectoryViewmodel(IMediaDirectory directory)
        {
            _directory = directory;
            _subdirectories = (directory as IIngestDirectory)?.SubDirectories != null
                ? ((IIngestDirectory)directory).SubDirectories.Select(d => new MediaDirectoryViewmodel((IIngestDirectory)d)).ToList()
                : new List<MediaDirectoryViewmodel>();
        }
        public IMediaDirectory Directory { get { return _directory; } }

        public bool IsOK { get { return _directory.IsInitialized == true && DirectoryFreePercentage >= 20; } }
        public long VolumeTotalSize { get { return _directory.VolumeFreeSize; } }
        public long VolumeFreeSize { get { return _directory.VolumeFreeSize; } }
        public float DirectoryFreePercentage
        {
            get
            {
                long totalSize = _directory.VolumeTotalSize;
                return (totalSize == 0) ? 0F : _directory.VolumeFreeSize * 100F / totalSize;
            }
        }
        public void SweepStaleMedia() { _directory.SweepStaleMedia(); }

        public bool IsInitialized { get { return _directory.IsInitialized; } }

        public string DirectoryName { get { return _directory.DirectoryName; } }

        public string Folder { get { return _directory.Folder; } }

        public bool IsIngestDirectory { get { return _directory is IIngestDirectory; } }

        public bool IsArchiveDirectory { get { return _directory is IArchiveDirectory; } }
        
        public bool IsServerDirectory { get { return _directory is IServerDirectory; } }

        public bool IsPersistentDirectory { get { return _directory is IServerDirectory || _directory is IArchiveDirectory; } }

        public bool IsAnimationDirectory { get { return _directory is IAnimationDirectory; } }
                
        public bool IsXdcam { get { return (_directory as IIngestDirectory)?.IsXDCAM == true; } }

        public bool IsWan { get { return (_directory as IIngestDirectory)?.IsWAN == true; } }

        public bool IsExport { get { return (_directory as IIngestDirectory)?.IsExport == true; } }

        public bool IsImport { get { return (_directory as IIngestDirectory)?.IsImport == true; } }

        public bool IsRecursive { get { return (_directory as IIngestDirectory)?.IsRecursive == true; } }
        
        public TDirectoryAccessType AccessType { get { return _directory is IIngestDirectory ? ((IIngestDirectory)_directory).AccessType : TDirectoryAccessType.Direct; } }

        public List<MediaDirectoryViewmodel> SubDirectories { get { return _subdirectories; } }

        public override string ToString()
        {
            return _directory.DirectoryName;
        }


    }
}
