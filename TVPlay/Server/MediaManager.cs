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
using TAS.Data;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using Newtonsoft.Json;

namespace TAS.Server
{

    [JsonObject(MemberSerialization.OptIn)]
    public class MediaManager: IMediaManager
    {
        readonly Guid _guidDto = Guid.NewGuid();
        readonly IEngine _engine;
        readonly FileManager _fileManager;
        public IFileManager FileManager { get { return _fileManager; } }
        public IEngine getEngine() { return _engine; }
        public IServerDirectory MediaDirectoryPGM { get; private set; }
        public IServerDirectory MediaDirectoryPRV { get; private set; }
        public IAnimationDirectory AnimationDirectoryPGM { get; private set; }
        public IAnimationDirectory AnimationDirectoryPRV { get; private set; }
        public IArchiveDirectory ArchiveDirectory { get; private set; }
        public readonly ObservableSynchronizedCollection<ITemplate> _templates = new ObservableSynchronizedCollection<ITemplate>();
        [JsonProperty]
        public VideoFormatDescription FormatDescription { get { return _engine.FormatDescription; } }
        [JsonProperty]
        public TVideoFormat VideoFormat { get { return _engine.VideoFormat; } }

        public MediaManager(Engine engine)
        {
            _engine = engine;
            _fileManager = new FileManager() { TempDirectory = new TempDirectory(this) };
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            MediaDirectoryPGM = (_engine.PlayoutChannelPGM == null) ? null : _engine.PlayoutChannelPGM.OwnerServer.MediaDirectory;
            MediaDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.MediaDirectory;
            AnimationDirectoryPGM = (_engine.PlayoutChannelPGM == null) ? null : _engine.PlayoutChannelPGM.OwnerServer.AnimationDirectory;
            AnimationDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.AnimationDirectory;

            ArchiveDirectory = this.LoadArchiveDirectory(_engine.IdArchive);
            Debug.WriteLine(this, "Begin initializing");
            ServerDirectory sdir = MediaDirectoryPGM as ServerDirectory;
            if (sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
                sdir.MediaSaved += _onServerDirectoryMediaSaved;
                sdir.MediaVerified += _mediaPGMVerified;
                sdir.MediaRemoved += _mediaPGMVerified;
            }
            sdir = MediaDirectoryPRV as ServerDirectory;
            if (MediaDirectoryPGM != MediaDirectoryPRV && sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
            }
            IAnimationDirectory adir = AnimationDirectoryPGM;
            if (adir != null)
                adir.PropertyChanged += _onAnimationDirectoryPropertyChanged;

            LoadIngestDirs(ConfigurationManager.AppSettings["IngestFolders"]);
            Debug.WriteLine(this, "End initializing");
        }
        [JsonProperty]
        public Guid GuidDto { get { return _guidDto; } }

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
                    || e.PropertyName == "HasExtraLines"
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
                    || e.PropertyName == "TCStart"
                    || e.PropertyName == "TCPlay"
                    || e.PropertyName == "VideoFormat"
                    || e.PropertyName == "AudioChannelMapping"
                    || e.PropertyName == "AudioLevelIntegrated"
                    || e.PropertyName == "AudioLevelPeak"
                    ))
            {
                ServerMedia compMedia = _findComplementaryMedia(media as ServerMedia);
                if (compMedia != null)
                {
                    PropertyInfo sourcePi = media.GetType().GetProperty(e.PropertyName);
                    PropertyInfo destPi = compMedia.GetType().GetProperty(e.PropertyName);
                    if (sourcePi != null && destPi != null)
                        destPi.SetValue(compMedia, sourcePi.GetValue(media, null), null);
                }
            }
        }

        private void _onServerDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInitialized")
            {
                ThreadPool.QueueUserWorkItem((o) => _synchronizePrvToPgm());
            }
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
            if (media is ServerMedia)
            {
                ServerMedia compMedia = _findComplementaryMedia(media as ServerMedia);
                if (compMedia != null)
                    ThreadPool.QueueUserWorkItem((o) =>
                        {
                            compMedia.Save();
                        }
                    );
            }
        }

        public void IngestMediaToPlayout(IMedia media, bool toTop = false)
        {
            if (media != null)
            {
                if (media is ServerMedia)
                {
                    if (media.MediaStatus == TMediaStatus.Deleted || media.MediaStatus == TMediaStatus.Required || media.MediaStatus == TMediaStatus.CopyError)
                    {
                        if (ArchiveDirectory != null)
                        {
                            IArchiveMedia fromArchive = ArchiveDirectory.Find(media);
                            if (fromArchive != null)
                            {
                                ArchiveDirectory.ArchiveRestore(fromArchive, media as ServerMedia, toTop);
                                return;
                            }
                        }
                        //IngestMedia im = FindIngestMedia(media);
                        //if (im != null)
                        //    ((IngestDirectory)im.Directory).IngestGet((IngestMedia)im, (ServerMedia)media, toTop);
                    }
                    return;
                }
                IServerMedia sm = null;
                if (MediaDirectoryPGM != null)
                    sm = MediaDirectoryPGM.GetServerMedia(media, true);
                if (sm != null
                    && (sm.MediaStatus == TMediaStatus.Deleted || sm.MediaStatus == TMediaStatus.Required || sm.MediaStatus == TMediaStatus.CopyError))
                {
                    if (media is ArchiveMedia)
                    {
                        ArchiveDirectory.ArchiveRestore((ArchiveMedia)media, sm, toTop);
                        return;
                    }
                    //if (media is IngestMedia)
                    //    ((IngestDirectory)media.Directory).IngestGet((IngestMedia)media, sm, toTop);
                }
            }
        }

        public void IngestMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false)
        {
            foreach (IMedia m in mediaList)
                IngestMediaToPlayout(m, ToTop);
        }

        public void IngestMediaToArchive(IIngestMedia media, bool toTop = false)
        {
            if (media == null)
                return;
            if (ArchiveDirectory != null)
            {
                IArchiveMedia destMedia;
                destMedia = ArchiveDirectory.GetArchiveMedia(media);
                if (!destMedia.FileExists())
                    _fileManager.Queue(new ConvertOperation { SourceMedia = media, DestMedia = destMedia, OutputFormat = _engine.VideoFormat }, toTop);
            }
        }

        public void IngestMediaToArchive(IArchiveMedia media, bool toTop = false)
        {
            if (media == null || ArchiveDirectory == null)
                return;
            Media sourceMedia = ((ArchiveMedia)media).OriginalMedia as Media;
            if (sourceMedia == null || !(sourceMedia is IIngestMedia))
                return;
            if (!media.FileExists() && sourceMedia.FileExists())
                _fileManager.Queue(new ConvertOperation { SourceMedia = sourceMedia, DestMedia = media, OutputFormat = _engine.VideoFormat }, toTop);
        }

        public void IngestMediaToArchive(IEnumerable<IIngestMedia> mediaList, bool ToTop = false)
        {
            foreach (IIngestMedia m in mediaList)
                IngestMediaToArchive(m);
        }

        private MediaDeleteDenyReason DeleteMedia(IMedia media)
        {
            MediaDeleteDenyReason reason = (media is ServerMedia) ? _engine.CanDeleteMedia(media as ServerMedia) : MediaDeleteDenyReason.NoDeny;
            if (reason.Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Delete, SourceMedia = media });
            return reason;
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IDto[] mediaList)
        {
            return mediaList.Select(m => DeleteMedia((Media)m));
        }

        public void GetLoudness(IMedia media)
        {
            media.GetLoudness();
        }

        public void GetLoudnessWithCallback(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
        {
            media.GetLoudnessWithCallback(startTime, duration, audioVolumeMeasuredCallback, finishCallback);
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            foreach (IMedia m in mediaList)
                m.GetLoudness();
        }

        public void ArchiveMedia(IMedia media, bool deleteAfter)
        {
            if (ArchiveDirectory == null)
                return;
            if (media is ServerMedia)
                ArchiveDirectory.ArchiveSave(media, _engine.VideoFormat, deleteAfter);
            if (media is IngestMedia)
                IngestMediaToArchive((IngestMedia)media, false);
            if (media is ArchiveMedia)
                IngestMediaToArchive((ArchiveMedia)media, false);
        }

        public void Queue(IFileOperation operation, bool toTop = false)
        {
            if (operation is FileOperation)
                _fileManager.Queue((FileOperation)operation, toTop);
            else
                if (operation is IConvertOperation)
            {
                _fileManager.Queue(new ConvertOperation()
                {
                    SourceMedia = operation.SourceMedia,
                    DestMedia = operation.DestMedia,

                }, toTop);
            }
            else
                throw new InvalidOperationException("Mediamanager.Queue invalid operation");
        }

        private ServerMedia _findComplementaryMedia(ServerMedia originalMedia)
        {
            if (_engine.PlayoutChannelPGM != null && _engine.PlayoutChannelPRV != null && _engine.PlayoutChannelPGM.OwnerServer != _engine.PlayoutChannelPRV.OwnerServer)
            {
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelPGM.OwnerServer && _engine.PlayoutChannelPRV != null)
                    return (ServerMedia)((MediaDirectory)_engine.PlayoutChannelPRV.OwnerServer.MediaDirectory).FindMedia(originalMedia);
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelPRV.OwnerServer && _engine.PlayoutChannelPGM != null)
                    return (ServerMedia)((MediaDirectory)_engine.PlayoutChannelPGM.OwnerServer.MediaDirectory).FindMedia(originalMedia);
            }
            return null;
        }

        private ServerMedia _getComplementaryMedia(ServerMedia originalMedia)
        {
            if (_engine.PlayoutChannelPGM != null && _engine.PlayoutChannelPRV != null && _engine.PlayoutChannelPGM.OwnerServer != _engine.PlayoutChannelPRV.OwnerServer)
            {
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelPGM.OwnerServer && _engine.PlayoutChannelPRV != null)
                    return (ServerMedia)_engine.PlayoutChannelPRV.OwnerServer.MediaDirectory.GetServerMedia(originalMedia);
                if ((originalMedia.Directory as ServerDirectory).Server == _engine.PlayoutChannelPRV.OwnerServer && _engine.PlayoutChannelPGM != null)
                    return (ServerMedia)_engine.PlayoutChannelPGM.OwnerServer.MediaDirectory.GetServerMedia(originalMedia);
            }
            return null;
        }

        private void _synchronizePrvToPgm()
        {
            if (MediaDirectoryPGM != null
                && MediaDirectoryPRV != null
                && MediaDirectoryPGM != MediaDirectoryPRV
                && MediaDirectoryPGM.IsInitialized
                && MediaDirectoryPRV.IsInitialized)
            {
                Debug.WriteLine(this, "_synchronizePrvToPgm");
                foreach (ServerMedia pGMmedia in MediaDirectoryPGM.Files.ToList())
                {
                    if (pGMmedia.MediaStatus == TMediaStatus.Available && pGMmedia.FileExists())
                    {
                        ServerMedia pRVmedia = (ServerMedia)((MediaDirectory)MediaDirectoryPRV).FindMedia(pGMmedia);
                        if (pRVmedia == null)
                        {
                            pRVmedia = (ServerMedia)MediaDirectoryPRV.Files.FirstOrDefault((m) => m.FileExists() && m.FileSize == pGMmedia.FileSize && m.FileName == pGMmedia.FileName && m.LastUpdated.DateTimeEqualToDays(pGMmedia.LastUpdated)); 
                            if (pRVmedia != null)
                            {
                                pRVmedia.CloneMediaProperties(pGMmedia);
                                pRVmedia.Save();
                            }
                            else
                            {
                                pRVmedia = (ServerMedia)MediaDirectoryPRV.GetServerMedia(pGMmedia, true);
                                _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Copy, SourceMedia = pGMmedia, DestMedia = pRVmedia });
                            }
                        }
                    }
                }
            }
        }

        private void _syncAnimations()
        {
            Debug.WriteLine(this, "_syncAnimations");
            if (AnimationDirectoryPGM != null
                && AnimationDirectoryPRV != null
                && AnimationDirectoryPGM != AnimationDirectoryPRV
                && AnimationDirectoryPGM.IsInitialized
                && AnimationDirectoryPRV.IsInitialized)
            {
                foreach (ServerMedia pGMmedia in AnimationDirectoryPGM.Files.ToList())
                {
                    if (pGMmedia.MediaStatus == TMediaStatus.Available)
                    {
                        var pRVmedia = (ServerMedia)MediaDirectoryPRV.Files.FirstOrDefault(m => m.Folder == pGMmedia.Folder && m.FileName == pGMmedia.FileName && m.LastUpdated.DateTimeEqualToDays(pGMmedia.LastUpdated));
                        if (pRVmedia != null)
                        {
                            pRVmedia.CloneMediaProperties(pGMmedia);
                            pRVmedia.Save();
                        }
                    }
                }
            }
        }
        
        public override string ToString()
        {
            return _engine.EngineName + ":MediaManager";
        }


        public void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory)
        {
            foreach (MediaExport e in exportList)
                Export(e, directory);

        }

        public void Export(MediaExport export, IIngestDirectory directory)
        {
            _fileManager.Queue(new XDCAM.ExportOperation() { SourceMedia = export.Media, StartTC = export.StartTC, Duration = export.Duration, AudioVolume = export.AudioVolume, DestDirectory = directory as IngestDirectory });
        }

        public Guid IngestFile(string fileName)
        {
            var nameLowered = fileName.ToLower();
            IServerMedia dest;
            if ((dest  = (ServerMedia)(((MediaDirectory)MediaDirectoryPGM).FindMedia(m => Path.GetFileNameWithoutExtension(m.FileName).ToLower() == nameLowered).FirstOrDefault())) != null)
                return dest.MediaGuid;
            foreach (IngestDirectory dir in _ingestDirectories)
            {
                Media source = dir.FindMedia(fileName);
                if (source != null)
                {
                    source.Verify();
                    if (source.MediaStatus == TMediaStatus.Available)
                    {
                        dest = MediaDirectoryPGM.GetServerMedia(source, false);
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

        private void _mediaPGMVerified(object o, MediaEventArgs e)
        {
            if (MediaDirectoryPRV != null
                && MediaDirectoryPRV != MediaDirectoryPGM
                && MediaDirectoryPRV.IsInitialized)
            {
                IServerMedia media = MediaDirectoryPRV.GetServerMedia(e.Media, true);
                if (media.FileSize == e.Media.FileSize
                    && media.FileName == e.Media.FileName
                    && media.FileSize == e.Media.FileSize
                    && !media.Verified)
                    media.Verify();
                if (!(media.MediaStatus == TMediaStatus.Available
                      || media.MediaStatus == TMediaStatus.Copying
                      || media.MediaStatus == TMediaStatus.CopyPending
                      || media.MediaStatus == TMediaStatus.Copied))
                    Queue(new FileOperation { Kind = TFileOperationKind.Copy, SourceMedia = e.Media, DestMedia = media });
            }
        }

        private void _mediaPGMRemoved(object o, MediaEventArgs e)
        {
            if (MediaDirectoryPRV != null
                && MediaDirectoryPRV != MediaDirectoryPGM
                && MediaDirectoryPRV.IsInitialized
                && !e.Media.FileExists())
            {
                IMedia media = ((MediaDirectory)MediaDirectoryPRV).FindMedia(e.Media);
                if (media != null && media.MediaStatus == TMediaStatus.Available)
                    Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = media });
            }
        }

        public IMedia GetPRVMedia(IMedia media)
        {
            if (MediaDirectoryPRV != null)
                return ((ServerDirectory)MediaDirectoryPRV).FindMedia(media);
            else
                return null;
        }
    }


}
