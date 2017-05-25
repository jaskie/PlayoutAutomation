using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaDirectoryViewmodel
    {
        readonly IMediaDirectory _directory;
        readonly List<MediaDirectoryViewmodel> _subdirectories;

        public MediaDirectoryViewmodel(IMediaDirectory directory, bool includeImport = false, bool includeExport = false)
        {
            _directory = directory;
            _subdirectories = (directory as IIngestDirectory)?.SubDirectories != null
                ? ((IIngestDirectory)directory)
                    .SubDirectories
                    .Where(d=> (includeImport && d.ContainsImport() )|| (includeExport && d.ContainsExport()))
                    .Select(d => new MediaDirectoryViewmodel((IIngestDirectory)d, includeImport, includeExport)).ToList()
                : new List<MediaDirectoryViewmodel>();
        }
        public IMediaDirectory Directory => _directory;

        public bool IsOK => _directory?.IsInitialized == true && DirectoryFreePercentage >= 20;
        public long VolumeTotalSize => _directory.VolumeTotalSize;
        public long VolumeFreeSize => _directory.VolumeFreeSize;

        public float DirectoryFreePercentage
        {
            get
            {
                long totalSize = _directory.VolumeTotalSize;
                return (totalSize == 0) ? 0F : _directory.VolumeFreeSize * 100F / totalSize;
            }
        }
        public void SweepStaleMedia() { _directory.SweepStaleMedia(); }

        public bool IsInitialized => _directory.IsInitialized;

        public string DirectoryName => _directory.DirectoryName;

        public string Folder => _directory.Folder;

        public bool IsIngestDirectory => _directory is IIngestDirectory;

        public bool IsArchiveDirectory => _directory is IArchiveDirectory;

        public bool IsServerDirectory => _directory is IServerDirectory;

        public bool IsPersistentDirectory => _directory is IServerDirectory || _directory is IArchiveDirectory;

        public bool IsAnimationDirectory => _directory is IAnimationDirectory;

        public bool IsXdcam => (_directory as IIngestDirectory)?.IsXDCAM == true;

        public bool IsWan => (_directory as IIngestDirectory)?.IsWAN == true;

        public bool IsExport => (_directory as IIngestDirectory)?.IsExport == true;

        public bool IsImport => (_directory as IIngestDirectory)?.IsImport == true;

        public bool ContainsImport { get { return IsImport || SubDirectories.Any(d => d.IsImport); } }

        public bool ContainsExport { get { return IsExport || SubDirectories.Any(d => d.IsExport); } }

        public TMovieContainerFormat? ExportContainerFormat => (_directory as IIngestDirectory)?.ExportContainerFormat;

        public TmXFAudioExportFormat MXFAudioExportFormat => (_directory as IIngestDirectory).MXFAudioExportFormat;

        public TmXFVideoExportFormat MXFVideoExportFormat => (_directory as IIngestDirectory).MXFVideoExportFormat;

        public bool IsRecursive => (_directory as IIngestDirectory)?.IsRecursive == true;

        public TDirectoryAccessType AccessType => _directory is IIngestDirectory ? ((IIngestDirectory)_directory).AccessType : TDirectoryAccessType.Direct;

        public List<MediaDirectoryViewmodel> SubDirectories => _subdirectories;

        public override string ToString()
        {
            return _directory.DirectoryName;
        }


    }
}
