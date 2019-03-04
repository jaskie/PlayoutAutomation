using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.ViewModels
{
    public class MediaDirectoryViewmodel
    {
        public MediaDirectoryViewmodel(IMediaDirectory directory, string directoryName, bool includeImport = false, bool includeExport = false)
        {
            Directory = directory;
            DirectoryName = directoryName;
            SubDirectories = (directory as IIngestDirectory)?.SubDirectories?.Where(d=> includeImport && d.ContainsImport() || includeExport && d.ContainsExport())
                             .Select(d => new MediaDirectoryViewmodel((IIngestDirectory)d, d.DirectoryName, includeImport, includeExport)).ToList() ?? new List<MediaDirectoryViewmodel>();
        }

        public IMediaDirectory Directory { get; }

        public bool IsOK => ((Directory as IWatcherDirectory)?.IsInitialized ?? true) && DirectoryFreePercentage >= 20;

        public long VolumeTotalSize => Directory.VolumeTotalSize;

        public long VolumeFreeSize => Directory.VolumeFreeSize;

        public float DirectoryFreePercentage
        {
            get
            {
                var totalSize = Directory.VolumeTotalSize;
                return totalSize == 0 ? 0F : Directory.VolumeFreeSize * 100F / totalSize;
            }
        }

        public void SweepStaleMedia() { (Directory as IWatcherDirectory)?.SweepStaleMedia(); }

        public string DirectoryName {get;}

        public string Folder => Directory.Folder;

        public bool IsIngestDirectory => Directory is IIngestDirectory;

        public bool IsArchiveDirectory => Directory is IArchiveDirectory;

        public bool IsServerDirectory => Directory is IServerDirectory;

        public bool IsPersistentDirectory => Directory is IServerDirectory || Directory is IArchiveDirectory;

        public bool IsAnimationDirectory => Directory is IAnimationDirectory;

        public bool IsXdcam => (Directory as IIngestDirectory)?.Kind == TIngestDirectoryKind.XDCAM;

        public bool IsWan => (Directory as IIngestDirectory)?.IsWAN == true;

        public bool IsExport => (Directory as IIngestDirectory)?.IsExport == true;

        public bool IsImport => (Directory as IIngestDirectory)?.IsImport == true;

        public bool ContainsImport { get { return IsImport || SubDirectories.Any(d => d.IsImport); } }

        public bool ContainsExport { get { return IsExport || SubDirectories.Any(d => d.IsExport); } }

        public TMovieContainerFormat? ExportContainerFormat => (Directory as IIngestDirectory)?.ExportContainerFormat;

        public TmXFAudioExportFormat MXFAudioExportFormat => (Directory as IIngestDirectory)?.MXFAudioExportFormat ?? TmXFAudioExportFormat.Channels4Bits16;

        public TmXFVideoExportFormat MXFVideoExportFormat => (Directory as IIngestDirectory)?.MXFVideoExportFormat ?? TmXFVideoExportFormat.IMX50;

        public bool IsRecursive => Directory is IArchiveDirectory || (Directory as IServerDirectory)?.IsRecursive == true || (Directory as IIngestDirectory)?.IsRecursive == true;

        public TDirectoryAccessType AccessType => (Directory as IIngestDirectory)?.AccessType ?? TDirectoryAccessType.Direct;

        public List<MediaDirectoryViewmodel> SubDirectories { get; }

        public override string ToString()
        {
            return DirectoryName;
        }


    }
}
