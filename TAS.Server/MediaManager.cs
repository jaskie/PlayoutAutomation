using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.IO;
using System.Configuration;
using System.Xml.Serialization;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.ServiceModel;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;
using Newtonsoft.Json;
using TAS.Remoting.Server;

namespace TAS.Server
{

    public class MediaManager: DtoBase, IMediaManager
    {
        readonly Engine _engine;
        readonly FileManager _fileManager;
        public IFileManager FileManager { get { return _fileManager; } }
        public IEngine getEngine() { return _engine; }
        public IServerDirectory MediaDirectoryPRI { get; private set; }
        public IServerDirectory MediaDirectorySEC { get; private set; }
        public IServerDirectory MediaDirectoryPRV { get; private set; }
        public IAnimationDirectory AnimationDirectoryPRI { get; private set; }
        public IAnimationDirectory AnimationDirectorySEC { get; private set; }
        public IAnimationDirectory AnimationDirectoryPRV { get; private set; }
        public IArchiveDirectory ArchiveDirectory { get; private set; }
        public readonly ObservableSynchronizedCollection<ITemplate> _templates = new ObservableSynchronizedCollection<ITemplate>();
        //[JsonProperty]
        public VideoFormatDescription FormatDescription { get { return _engine.FormatDescription; } }
        [JsonProperty]
        public TVideoFormat VideoFormat { get { return _engine.VideoFormat; } }
        public double VolumeReferenceLoudness { get { return _engine.VolumeReferenceLoudness; } }

        public MediaManager(Engine engine)
        {
            _engine = engine;
            _fileManager = new FileManager() { TempDirectory = new TempDirectory(this) };
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            ArchiveDirectory = this.LoadArchiveDirectory<ArchiveDirectory>(_engine.IdArchive);
            MediaDirectoryPRI = (_engine.PlayoutChannelPRI == null) ? null : _engine.PlayoutChannelPRI.OwnerServer.MediaDirectory;
            MediaDirectorySEC = (_engine.PlayoutChannelSEC == null) ? null : _engine.PlayoutChannelSEC.OwnerServer.MediaDirectory;
            MediaDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.MediaDirectory;
            AnimationDirectoryPRI = (_engine.PlayoutChannelPRI == null) ? null : _engine.PlayoutChannelPRI.OwnerServer.AnimationDirectory;
            AnimationDirectorySEC = (_engine.PlayoutChannelSEC == null) ? null : _engine.PlayoutChannelSEC.OwnerServer.AnimationDirectory;
            AnimationDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.AnimationDirectory;
            if (MediaDirectoryPRI != null)
                MediaDirectoryPRI.Initialize();
            if (MediaDirectorySEC != null)
                MediaDirectorySEC.Initialize();
            if (MediaDirectoryPRV != null)
                MediaDirectoryPRV.Initialize();
            if (ArchiveDirectory != null)
            {
                ArchiveDirectory.Initialize();
                ArchiveDirectory.MediaDeleted += ArchiveDirectory_MediaDeleted;
            }
            Debug.WriteLine(this, "Begin initializing");
            ServerDirectory sdir = MediaDirectoryPRI as ServerDirectory;
            if (sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
                sdir.MediaSaved += _onServerDirectoryMediaSaved;
                sdir.MediaVerified += _mediaPRIVerified;
                sdir.MediaRemoved += _mediaPRIRemoved;
            }
            sdir = MediaDirectorySEC as ServerDirectory;
            if (MediaDirectoryPRI != MediaDirectorySEC && sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
            }
            IAnimationDirectory adir = AnimationDirectoryPRI;
            if (adir != null)
                adir.PropertyChanged += _onAnimationDirectoryPropertyChanged;

            LoadIngestDirs(ConfigurationManager.AppSettings["IngestFolders"]);
            _fileManager.VolumeReferenceLoudness =  Convert.ToDecimal(VolumeReferenceLoudness);
            Debug.WriteLine(this, "End initializing");
        }

        private void ArchiveDirectory_MediaDeleted(object sender, MediaEventArgs e)
        {
            if (MediaDirectoryPRI != null)
            {
                var m = ((ServerDirectory)MediaDirectoryPRI).FindMediaByMediaGuid(e.Media.MediaGuid) as ServerMedia;
                if (m != null)
                    m.IsArchived = false;
            }
        }

        private List<IIngestDirectory> _ingestDirectories;
        public List<IIngestDirectory> IngestDirectories
        {
            get
            {
                lock (_ingestDirsSyncObject)
                    return _ingestDirectories.ToList();
            }
        }

        private bool _ingestDirectoriesLoaded = false;
        private object _ingestDirsSyncObject = new object();


        public void ReloadIngestDirs()
        {
            foreach (IngestDirectory d in _ingestDirectories)
                d.Dispose();
            LoadIngestDirs(ConfigurationManager.AppSettings["IngestFolders"]);
            Debug.WriteLine(this, "IngestDirectories reloaded");
        }

        public void LoadIngestDirs(string fileName)
        {
            lock (_ingestDirsSyncObject)
            {
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    if (_ingestDirectoriesLoaded)
                        return;
                    XmlSerializer reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
                    System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                    _ingestDirectories = ((List<IngestDirectory>)reader.Deserialize(file)).Cast<IIngestDirectory>().ToList();
                    file.Close();
                }
                else _ingestDirectories = new List<IIngestDirectory>();
                _ingestDirectoriesLoaded = true;
                foreach (IngestDirectory d in _ingestDirectories)
                {
                    d.MediaManager = this;
                    d.Initialize();
                }
            }
        }

        private void ServerMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (media is ServerMedia
                && !string.IsNullOrEmpty(e.PropertyName)
                   && (e.PropertyName == "DoNotArchive"
                    || e.PropertyName == "IdAux"
                    || e.PropertyName == "idFormat"
                    || e.PropertyName == "idProgramme"
                    || e.PropertyName == "KillDate"
                    || e.PropertyName == "OriginalMedia"
                    || e.PropertyName == "AudioVolume"
                    || e.PropertyName == "MediaCategory"
                    || e.PropertyName == "Parental"
                    || e.PropertyName == "MediaEmphasis"
                    || e.PropertyName == "FileName"
                    || e.PropertyName == "MediaName"
                    || e.PropertyName == "Duration"
                    || e.PropertyName == "DurationPlay"
                    || e.PropertyName == "TcStart"
                    || e.PropertyName == "TcPlay"
                    || e.PropertyName == "VideoFormat"
                    || e.PropertyName == "AudioChannelMapping"
                    || e.PropertyName == "AudioLevelIntegrated"
                    || e.PropertyName == "AudioLevelPeak"
                    || e.PropertyName == "IsArchived"
                    || e.PropertyName == "Protected"
                    ))
            {
                ServerMedia compMedia = _findComplementaryMedia(media as ServerMedia);
                if (compMedia != null)
                {
                    if (e.PropertyName == "FileName")
                        compMedia.RenameTo(((ServerMedia)media).FileName);
                    else
                    {
                        PropertyInfo sourcePi = media.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = compMedia.GetType().GetProperty(e.PropertyName);
                        if (sourcePi != null && destPi != null)
                            destPi.SetValue(compMedia, sourcePi.GetValue(media, null), null);
                    }
                }
            }
        }

        private void _onServerDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInitialized")
                SynchronizeSecToPri(false);
        }

        private void _onAnimationDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInitialized")
            {
                ThreadPool.QueueUserWorkItem((o) => _syncAnimations());
            }
        }

        private void _onServerDirectoryMediaSaved(object media, MediaEventArgs e)
        {
            ServerMedia priMedia = media as ServerMedia;
            if (priMedia != null && priMedia.MediaStatus != TMediaStatus.Deleted)
            {
                ServerMedia compMedia = _findComplementaryMedia(priMedia);
                if (compMedia != null)
                    ThreadPool.QueueUserWorkItem((o) =>
                        {
                            compMedia.Save();
                        }
                    );
            }
        }

        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList, bool toTop)
        {
            foreach (IMedia sourceMedia in mediaList)
            {
                if (sourceMedia is ArchiveMedia)
                {
                    IServerDirectory destDir = MediaDirectoryPRI != null && MediaDirectoryPRI.DirectoryExists() ? MediaDirectoryPRI :
                                               MediaDirectoryPRV != null && MediaDirectoryPRV.DirectoryExists() ? MediaDirectoryPRV :
                                               null;
                    if (destDir != null)
                    {
                        IMedia destMedia = destDir.GetServerMedia(sourceMedia, true);
                        if (!destMedia.FileExists())
                            _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Copy, SourceMedia = sourceMedia, DestMedia = destMedia }, toTop);
                    }
                }
            }
        }
            

        private MediaDeleteDenyReason deleteMedia(IMedia media)
        {
            MediaDeleteDenyReason reason = (media is PersistentMedia) ? _engine.CanDeleteMedia(media as PersistentMedia) : MediaDeleteDenyReason.NoDeny;
            if (reason.Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Delete, SourceMedia = media });
            return reason;
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList)
        {
            List<MediaDeleteDenyReason> result = new List<MediaDeleteDenyReason>();
            foreach (var media in mediaList)
                result.Add(deleteMedia(media));
            return result;
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            foreach (IMedia m in mediaList)
                m.GetLoudness();
        }

        public void ArchiveMedia(IEnumerable<IServerMedia> mediaList, bool deleteAfter)
        {
            IArchiveDirectory adir = ArchiveDirectory;
            if (adir == null)
                return;
            foreach (IServerMedia media in mediaList)
                if (media is ServerMedia)
                    adir.ArchiveSave(media, deleteAfter);
        }

        private ServerMedia _findComplementaryMedia(ServerMedia originalMedia)
        {
            if (_engine.PlayoutChannelPRI != null && _engine.PlayoutChannelSEC != null && _engine.PlayoutChannelPRI.OwnerServer != _engine.PlayoutChannelSEC.OwnerServer)
            {
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelPRI.OwnerServer && _engine.PlayoutChannelSEC != null)
                    return (ServerMedia)((MediaDirectory)_engine.PlayoutChannelSEC.OwnerServer.MediaDirectory).FindMediaByMediaGuid(originalMedia.MediaGuid);
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelSEC.OwnerServer && _engine.PlayoutChannelPRI != null)
                    return (ServerMedia)((MediaDirectory)_engine.PlayoutChannelPRI.OwnerServer.MediaDirectory).FindMediaByMediaGuid(originalMedia.MediaGuid);
            }
            return null;
        }

        public void SynchronizeSecToPri(bool deleteNotExisted)
        {
            if (MediaDirectoryPRI != null
                && MediaDirectoryPRV != null
                && MediaDirectoryPRI != MediaDirectoryPRV
                && MediaDirectoryPRI.IsInitialized
                && MediaDirectoryPRV.IsInitialized)
            {
                ThreadPool.QueueUserWorkItem(o =>
               {
                   Debug.WriteLine(this, "SynchronizeSecToPri started");
                   var pRIMediaList = MediaDirectoryPRI.GetFiles().ToList();
                   foreach (ServerMedia pRImedia in pRIMediaList)
                   {
                       if (pRImedia.MediaStatus == TMediaStatus.Available && pRImedia.FileExists())
                       {
                           ServerMedia pRVmedia = (ServerMedia)((MediaDirectory)MediaDirectoryPRV).FindMediaByMediaGuid(pRImedia.MediaGuid);
                           if (pRVmedia == null)
                           {
                               pRVmedia = (ServerMedia)((ServerDirectory)MediaDirectoryPRV).FindMediaFirst(m => m.FileExists() && m.FileSize == pRImedia.FileSize && m.FileName == pRImedia.FileName && m.LastUpdated.DateTimeEqualToDays(pRImedia.LastUpdated));
                               if (pRVmedia != null)
                               {
                                   pRVmedia.CloneMediaProperties(pRImedia);
                                   pRVmedia.Verify();
                               }
                               else
                               {
                                   pRVmedia = (ServerMedia)MediaDirectoryPRV.GetServerMedia(pRImedia, true);
                                   _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Copy, SourceMedia = pRImedia, DestMedia = pRVmedia });
                               }
                           }
                       }
                   }
                   if (deleteNotExisted)
                   {
                       var prvMediaList = MediaDirectoryPRV.GetFiles().ToList();
                       foreach (ServerMedia prvMedia in prvMediaList)
                       {
                           if ((ServerMedia)((MediaDirectory)MediaDirectoryPRI).FindMediaByMediaGuid(prvMedia.MediaGuid) == null)
                               _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Delete, SourceMedia = prvMedia });
                       }
                       var duplicatesList = prvMediaList.Where(m => prvMediaList.FirstOrDefault(d => d.MediaGuid == m.MediaGuid && ((ServerMedia)d).IdPersistentMedia != ((ServerMedia)m).IdPersistentMedia) != null).Select(m => m.MediaGuid).Distinct();
                       foreach(var mediaGuid in duplicatesList)
                           ((MediaDirectory)MediaDirectoryPRV)
                           .FindMediaList(m => m.MediaGuid == mediaGuid)
                           .Skip(1).ToList()
                           .ForEach(m => m.Delete());
                   }
               });
            }
        }

        private void _syncAnimations()
        {
            Debug.WriteLine(this, "_syncAnimations");
            if (AnimationDirectoryPRI != null
                && AnimationDirectorySEC != null
                && AnimationDirectorySEC != AnimationDirectoryPRV
                && AnimationDirectoryPRI.IsInitialized
                && AnimationDirectorySEC.IsInitialized)
            {
                foreach (ServerMedia pRImedia in AnimationDirectoryPRI.GetFiles())
                {
                    if (pRImedia.MediaStatus == TMediaStatus.Available)
                    {
                        var sECmedia = (ServerMedia)MediaDirectorySEC.GetFiles().FirstOrDefault(m => m.Folder == pRImedia.Folder && m.FileName == pRImedia.FileName && m.LastUpdated.DateTimeEqualToDays(pRImedia.LastUpdated));
                        if (sECmedia != null)
                        {
                            sECmedia.CloneMediaProperties(pRImedia);
                            sECmedia.Save();
                        }
                    }
                }
            }
        }
        
        public override string ToString()
        {
            return _engine.EngineName + ":MediaManager";
        }


        public void Export(IEnumerable<ExportMedia> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory)
        {
            if (asSingleFile)
            {
                _fileManager.Queue(new ExportOperation() { ExportMediaList = exportList, DestMediaName = singleFilename, DestDirectory = directory as IngestDirectory });
            }
            else
                foreach (ExportMedia e in exportList)
                    Export(e, directory);
        }

        private void Export(ExportMedia export, IIngestDirectory directory)
        {
            _fileManager.Queue(new ExportOperation() { ExportMediaList = new[] { export }, DestMediaName = export.Media.MediaName, StartTC = export.StartTC, Duration = export.Duration, AudioVolume = export.AudioVolume, DestDirectory = directory as IngestDirectory });
        }

        public Guid IngestFile(string fileName)
        {
            var nameLowered = fileName.ToLower();
            IServerMedia dest;
            if ((dest  = (ServerMedia)(((MediaDirectory)MediaDirectoryPRI).FindMediaList(m => Path.GetFileNameWithoutExtension(m.FileName).ToLower() == nameLowered).FirstOrDefault())) != null)
                return dest.MediaGuid;
            foreach (IngestDirectory dir in _ingestDirectories)
            {
                Media source = dir.FindMedia(fileName);
                if (source != null)
                {
                    source.Verify();
                    if (source.MediaStatus == TMediaStatus.Available)
                    {
                        dest = MediaDirectoryPRI.GetServerMedia(source, false);
                        _fileManager.Queue(new ConvertOperation()
                        {
                            SourceMedia = source,
                            DestMedia = dest,
                            OutputFormat = _engine.VideoFormat,
                            AudioVolume = dir.AudioVolume,
                            SourceFieldOrderEnforceConversion = dir.SourceFieldOrder,
                            AspectConversion = dir.AspectConversion,
                        });
                        return dest.MediaGuid;
                    }
                }
            }
            return Guid.Empty;            
        }

        private void _mediaPRIVerified(object o, MediaEventArgs e)
        {
            if (MediaDirectorySEC != null
                && MediaDirectorySEC != MediaDirectoryPRI
                && MediaDirectorySEC.IsInitialized)
            {
                IMedia pRIMedia = e.Media;
                IServerMedia media = MediaDirectorySEC.GetServerMedia(pRIMedia, true);
                if (media.FileSize == pRIMedia.FileSize
                    && media.FileName == pRIMedia.FileName
                    && media.FileSize == pRIMedia.FileSize
                    && !media.Verified)
                    ((Media)media).Verify();
                if (!(media.MediaStatus == TMediaStatus.Available
                      || media.MediaStatus == TMediaStatus.Copying
                      || media.MediaStatus == TMediaStatus.CopyPending
                      || media.MediaStatus == TMediaStatus.Copied))
                    FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Copy, SourceMedia = pRIMedia, DestMedia = media }, false);
            }
        }

        private void _mediaPRIRemoved(object o, MediaEventArgs e)
        {
            if (MediaDirectorySEC != null
                && MediaDirectorySEC != MediaDirectoryPRI
                && MediaDirectorySEC.IsInitialized)
            {
                IMedia mediaToDelete = ((MediaDirectory)MediaDirectorySEC).FindMediaByMediaGuid(e.Media.MediaGuid);
                if (mediaToDelete != null && mediaToDelete.FileExists())
                    FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = mediaToDelete }, false);
            }
        }
    }


}
