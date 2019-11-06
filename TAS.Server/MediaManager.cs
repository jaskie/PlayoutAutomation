using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ComponentModelRPC.Server;
using TAS.Common;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;
using TAS.Server.MediaOperation;

namespace TAS.Server
{
    public class MediaManager : DtoBase, IMediaManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [JsonProperty(nameof(Engine))]
        private readonly Engine _engine;
        [JsonProperty(nameof(FileManager))]
        private readonly FileManager _fileManager;
        private readonly List<CasparRecorder> _recorders;
        private List<IngestDirectory> _ingestDirectories;
        private int _isInitialMediaSecToPriSynchronized;
        private bool _isSynchronizedAnimationsSecToPri;

        public MediaManager(Engine engine)
        {
            _engine = engine;
            _recorders = new List<CasparRecorder>();
            _fileManager = new FileManager(new TempDirectory());
        }

        public IFileManager FileManager => _fileManager;

        public IEngine Engine => _engine;

        [JsonProperty]
        public IServerDirectory MediaDirectoryPRI { get; private set; }

        [JsonProperty]
        public IServerDirectory MediaDirectorySEC { get; private set; }

        [JsonProperty]
        public IServerDirectory MediaDirectoryPRV { get; private set; }

        [JsonProperty]
        public IAnimationDirectory AnimationDirectoryPRI { get; private set; }

        [JsonProperty]
        public IAnimationDirectory AnimationDirectorySEC { get; private set; }

        [JsonProperty]
        public IAnimationDirectory AnimationDirectoryPRV { get; private set; }

        [JsonProperty]
        public IArchiveDirectory ArchiveDirectory { get; private set; }

        public ICGElementsController CGElementsController => _engine.CGElementsController;

        [JsonProperty(IsReference = false)]
        public VideoFormatDescription FormatDescription => _engine.FormatDescription;

        [JsonProperty]
        public TVideoFormat VideoFormat => _engine.VideoFormat;

        [JsonProperty]
        public IEnumerable<IIngestDirectory> IngestDirectories => _ingestDirectories;

        [JsonProperty]
        public IEnumerable<IRecorder> Recorders => _recorders;

        public void Initialize()
        {
            Debug.WriteLine(this, "Begin initializing");
            Logger.Debug("Begin initializing");
            _fileManager.ReferenceLoudness = _engine.VolumeReferenceLoudness;
            var archiveDirectory = EngineController.Database.LoadArchiveDirectory<ArchiveDirectory>(_engine.IdArchive);
            if (archiveDirectory != null)
            {
                archiveDirectory.Initialize(this);
                archiveDirectory.RefreshVolumeInfo();
            }
            ArchiveDirectory = archiveDirectory;
            MediaDirectoryPRI = ((CasparServerChannel)_engine.PlayoutChannelPRI)?.Owner.MediaDirectory;
            MediaDirectorySEC = ((CasparServerChannel)_engine.PlayoutChannelSEC)?.Owner.MediaDirectory;
            MediaDirectoryPRV = ((CasparServerChannel)_engine.PlayoutChannelPRV)?.Owner.MediaDirectory;
            AnimationDirectoryPRI = ((CasparServerChannel)_engine.PlayoutChannelPRI)?.Owner.AnimationDirectory;
            AnimationDirectorySEC = ((CasparServerChannel)_engine.PlayoutChannelSEC)?.Owner.AnimationDirectory;
            AnimationDirectoryPRV = ((CasparServerChannel)_engine.PlayoutChannelPRV)?.Owner.AnimationDirectory;
            IWatcherDirectory[] initializationList = { MediaDirectoryPRI, MediaDirectorySEC, MediaDirectoryPRV, AnimationDirectoryPRI, AnimationDirectorySEC, AnimationDirectoryPRV };
            foreach (var mediaDirectory in initializationList.Distinct())
                (mediaDirectory as WatcherDirectory)?.Initialize(this);

            if (MediaDirectoryPRI is ServerDirectory sdir)
            {
                sdir.MediaPropertyChanged += _serverMediaPropertyChanged;
                sdir.PropertyChanged += _serverDirectoryPropertyChanged;
                sdir.MediaSaved += _serverDirectoryMediaSaved;
                sdir.MediaVerified += _mediaPRIVerified;
                sdir.MediaRemoved += _mediaPRIRemoved;
            }
            sdir = MediaDirectorySEC as ServerDirectory;
            if (MediaDirectoryPRI != MediaDirectorySEC && sdir != null)
            {
                sdir.MediaPropertyChanged += _serverMediaPropertyChanged;
                sdir.PropertyChanged += _serverDirectoryPropertyChanged;
                sdir.MediaSaved += _serverDirectoryMediaSaved;
            }
            if (AnimationDirectoryPRI is AnimationDirectory adir)
            {
                adir.MediaSaved += _animationDirectoryMediaSaved;
                adir.PropertyChanged += _animationDirectoryPropertyChanged;
                adir.MediaAdded += _animationDirectoryMediaAdded;
                adir.MediaRemoved += _animationDirectoryMediaRemoved;
                adir.MediaPropertyChanged += _animationDirectoryMediaPropertyChanged;
            }
            adir = AnimationDirectorySEC as AnimationDirectory;
            if (adir != null)
                adir.PropertyChanged += _animationDirectoryPropertyChanged;

            LoadIngestDirs();
            Debug.WriteLine(this, "End initializing");
            Logger.Debug("End initializing");
        }

        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList)
        {
            var destDir = MediaDirectoryPRI != null && MediaDirectoryPRI.DirectoryExists() ? (ServerDirectory)MediaDirectoryPRI :
                MediaDirectoryPRV != null && MediaDirectoryPRV.DirectoryExists() ? (ServerDirectory)MediaDirectoryPRV :
                    throw new ApplicationException("No ServerDirectory available to copy media to");
            foreach (var sourceMedia in mediaList)
            {
                if (destDir.FindMediaByMediaGuid(sourceMedia.MediaGuid) != null)
                    continue;
                if (sourceMedia is PersistentMedia && destDir != null && destDir != sourceMedia.Directory)
                    _fileManager.Queue(new CopyOperation(_fileManager) { Source = sourceMedia, DestDirectory = destDir });
            }
        }

        public List<MediaDeleteResult> MediaDelete(IEnumerable<IMedia> mediaList, bool forceDelete)
        {
            if (!Engine.HaveRight(EngineRight.MediaDelete))
                return new List<MediaDeleteResult>(mediaList.Select(m => new MediaDeleteResult() {Media = m, Result = MediaDeleteResult.MediaDeleteResultEnum.InsufficentRights }));

            var result = new List<MediaDeleteResult>();
            foreach (var media in mediaList)
                result.Add(_deleteMedia(media, forceDelete));
            return result;
        }

        public IServerDirectory DetermineValidServerDirectory()
        {
            var pri = MediaDirectoryPRI;
            var sec = MediaDirectorySEC;
            if (pri?.DirectoryExists() == true)
                return pri;
            if (sec?.DirectoryExists() == true)
                return sec;
            return null;
        }

        public void MeasureLoudness(IEnumerable<IMedia> mediaList)
        {
            if (!Engine.HaveRight(EngineRight.MediaEdit))
                return;
            foreach (IMedia m in mediaList)
                m.GetLoudness();
        }

        public List<MediaDeleteResult> MediaArchive(IEnumerable<IMedia> mediaList, bool deleteAfter, bool forceDelete)
        {
            var result = new List<MediaDeleteResult>();
            if (!Engine.HaveRight(EngineRight.MediaArchive) || 
                (deleteAfter && !Engine.HaveRight(EngineRight.MediaDelete)))
                return result;
            if (!(ArchiveDirectory is ArchiveDirectory adir))
                return result;
            foreach (var media in mediaList)
                if (media is ServerMedia serverMedia)
                {
                    if (forceDelete || !deleteAfter)
                    {
                        adir.ArchiveSave(serverMedia, deleteAfter);
                        result.Add(MediaDeleteResult.NoDeny);
                        continue;
                    }
                    var dr = _engine.CanDeleteMedia(serverMedia);
                    if (dr.Result == MediaDeleteResult.MediaDeleteResultEnum.Success)
                        adir.ArchiveSave(serverMedia, true);
                    result.Add(dr);
                }
                    
            return result;
        }

        public void Export(IEnumerable<MediaExportDescription> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat)
        {
            if (!Engine.HaveRight(EngineRight.MediaExport))
                return;
            
            if (asSingleFile)
            {
                _fileManager.Queue(new ExportOperation(_fileManager) { Sources = exportList, DestProperties = new MediaProxy{FileName = singleFilename, VideoFormat = Engine.VideoFormat, MediaName = Path.GetFileNameWithoutExtension(singleFilename)}, DestDirectory = directory as IngestDirectory, MXFAudioExportFormat = mXFAudioExportFormat, MXFVideoExportFormat = mXFVideoExportFormat });
            }
            else
                foreach (MediaExportDescription e in exportList)
                    _export(e, directory, mXFAudioExportFormat, mXFVideoExportFormat);
        }

        public void LoadIngestDirs()
        {
            _loadIngestDirs(Path.Combine(Directory.GetCurrentDirectory(), ConfigurationManager.AppSettings["IngestFolders"]));
            Debug.WriteLine(this, "IngestDirectories loaded");
        }

        public void UnloadIngestDirs()
        {
            foreach (var d in _ingestDirectories)
                d.Dispose();
            _ingestDirectories = null;
        }

        public async void SynchronizeMediaSecToPri()
        {
            if (!(MediaDirectoryPRI is ServerDirectory pri) || !(MediaDirectorySEC is ServerDirectory sec) ||
                pri == sec || !pri.IsInitialized || !sec.IsInitialized)
                return;
            try
            {
                await Task.Run(() => CopyMissingMediaPriToSec(pri, sec));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            try
            {
                await Task.Run(() => DeleteExtraSecMedia(pri, sec));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }


        public void SynchronizeAnimationsSecToPri()
        {
            if (AnimationDirectoryPRI is AnimationDirectory pri
                && AnimationDirectorySEC is AnimationDirectory sec
                && pri != sec
                && pri.IsInitialized
                && sec.IsInitialized)
            {
                Task.Run(() =>
                {
                    if (_isSynchronizedAnimationsSecToPri)
                        return;
                    try
                    {
                        Debug.WriteLine(this, "SynchronizeAnimationsSecToPri started");
                        Logger.Debug("SynchronizeAnimationsSecToPri started");
                        var priAnimations = pri.GetAllFiles();
                        foreach (var priAnimation in priAnimations)
                        {
                            if (priAnimation.MediaStatus != TMediaStatus.Available)
                                continue;
                            if (sec.FindMediaByMediaGuid(priAnimation.MediaGuid) is AnimatedMedia)
                                continue;
                            var sEcAnimation = (AnimatedMedia) sec.FindMediaFirst(m =>
                                m.Folder == priAnimation.Folder && m.FileName == priAnimation.FileName &&
                                priAnimations.All(a => a.MediaGuid != m.MediaGuid));
                            if (sEcAnimation != null)
                            {
                                sEcAnimation.CloneMediaProperties(priAnimation);
                                sec.UpdateMediaGuid(sEcAnimation, priAnimation.MediaGuid);
                                sEcAnimation.Save();
                                Debug.WriteLine(sEcAnimation, "Updated");
                            }
                            else
                            {
                                var secFileName = Path.Combine(sec.Folder, priAnimation.Folder, priAnimation.FileName);
                                if (File.Exists(secFileName))
                                    sec.CloneMedia((IAnimatedMedia) priAnimation, priAnimation.MediaGuid);
                            }
                        }
                        _isSynchronizedAnimationsSecToPri = true;
                        Logger.Debug("SynchronizeAnimationsSecToPri finished");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "SynchronizeAnimationsSecToPri exception");
                    }

                });
            }
        }

        public override string ToString()
        {
            return _engine.EngineName + ":MediaManager";
        }

        internal void SetRecorders(List<CasparRecorder> recorders)
        {
            foreach (var recorder in _recorders)
                recorder.CaptureSuccess -= _recorder_CaptureSuccess;
            _recorders.Clear();
            foreach (var recorder in recorders)
            {
                recorder.ArchiveDirectory = ArchiveDirectory;
                _recorders.Add(recorder);
                recorder.CaptureSuccess += _recorder_CaptureSuccess;
            }
        }


        // private methods

        private void _loadIngestDirs(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                if (_ingestDirectories != null)
                    return;
                var reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute(nameof(IngestDirectories)));
                using (var file = new StreamReader(fileName))
                    _ingestDirectories = ((List<IngestDirectory>)reader.Deserialize(file)).ToList();
            }
            else _ingestDirectories = new List<IngestDirectory>();
            foreach (var d in _ingestDirectories)
            {
                d.MediaManager = this;
                d.Initialize(this);
            }
        }

        private void _serverMediaPropertyChanged(object dir, MediaPropertyChangedEventArgs e)
        {
            var adirPri = MediaDirectoryPRI;
            var adirSec = MediaDirectorySEC;
            if (!(e.Media is ServerMedia media) ||
                adirPri == null || adirSec == null || adirPri == adirSec ||
                (
                    e.PropertyName != nameof(IServerMedia.DoNotArchive) &&
                    e.PropertyName != nameof(IServerMedia.IdAux) &&
                    e.PropertyName != nameof(IServerMedia.IdProgramme) &&
                    e.PropertyName != nameof(IServerMedia.KillDate) &&
                    e.PropertyName != nameof(IServerMedia.AudioVolume) &&
                    e.PropertyName != nameof(IServerMedia.MediaCategory) &&
                    e.PropertyName != nameof(IServerMedia.Parental) &&
                    e.PropertyName != nameof(IServerMedia.MediaEmphasis) &&
                    e.PropertyName != nameof(IServerMedia.FileName) &&
                    e.PropertyName != nameof(IServerMedia.MediaName) &&
                    e.PropertyName != nameof(IServerMedia.Duration) &&
                    e.PropertyName != nameof(IServerMedia.DurationPlay) &&
                    e.PropertyName != nameof(IServerMedia.TcStart) &&
                    e.PropertyName != nameof(IServerMedia.TcPlay) &&
                    e.PropertyName != nameof(IServerMedia.VideoFormat) &&
                    e.PropertyName != nameof(IServerMedia.AudioChannelMapping) &&
                    e.PropertyName != nameof(IServerMedia.AudioLevelIntegrated) &&
                    e.PropertyName != nameof(IServerMedia.AudioLevelPeak) &&
                    e.PropertyName != nameof(IServerMedia.IsArchived) &&
                    e.PropertyName != nameof(IServerMedia.IsProtected) &&
                    e.PropertyName != nameof(IServerMedia.FieldOrderInverted) && 
                    e.PropertyName != nameof(IServerMedia.IsArchived)
                ))
                return;
            var compMedia = _findComplementaryMedia(media);
            if (compMedia == null)
                return;
            var pi = typeof(ServerMedia).GetProperty(e.PropertyName);
            if (pi == null || !pi.CanWrite)
                return;
            if (e.PropertyName == nameof(IServerMedia.FileName) && pi.GetValue(media, null) is string newFileName)
                compMedia.RenameFileTo(newFileName);
            else
                pi.SetValue(compMedia, pi.GetValue(media, null), null);
        }

        private void _serverDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IServerDirectory.IsInitialized))
                InitialMediaSynchronization();
        }

        private void _animationDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IAnimationDirectory.IsInitialized))
                SynchronizeAnimationsSecToPri();
        }

        private void _animationDirectoryMediaSaved(object sender, MediaEventArgs e)
        {
            if (!(e.Media is AnimatedMedia priMedia))
                throw new ApplicationException("Invalid media type provided");
            if (priMedia.MediaStatus == TMediaStatus.Deleted)
                return;
            if (!(AnimationDirectoryPRI is AnimationDirectory pri)
                || !(AnimationDirectorySEC is AnimationDirectory sec)
                || sec == pri
                || !sec.IsInitialized
                || !pri.IsInitialized)
                return;
            var compMedia = (e.Media.Directory == pri
                ? sec.FindMediaByMediaGuid(e.Media.MediaGuid)
                : pri.FindMediaByMediaGuid(e.Media.MediaGuid)) 
                as AnimatedMedia;
            if (compMedia?.IsModified == true)
                compMedia.Save();
        }

        private void _animationDirectoryMediaPropertyChanged(object o, MediaPropertyChangedEventArgs e)
        {
            if (!(e.Media is AnimatedMedia media
                 && AnimationDirectoryPRI is AnimationDirectory adirPri
                 && AnimationDirectorySEC is AnimationDirectory adirSec 
                 && adirPri != adirSec 
                 && (e.PropertyName == nameof(IAnimatedMedia.MediaName) 
                    || e.PropertyName == nameof(IAnimatedMedia.Fields) 
                    || e.PropertyName == nameof(IAnimatedMedia.Method) 
                    || e.PropertyName == nameof(IAnimatedMedia.TemplateLayer) 
                    || e.PropertyName == nameof(IAnimatedMedia.ScheduledDelay)
                    || e.PropertyName == nameof(IAnimatedMedia.StartType)
                    )))
                return;
            if (!(adirSec.FindMediaByMediaGuid(media.MediaGuid) is AnimatedMedia compMedia))
                return;
            var sourcePi = media.GetType().GetProperty(e.PropertyName);
            var destPi = compMedia.GetType().GetProperty(e.PropertyName);
            if (sourcePi != null && destPi != null)
                destPi.SetValue(compMedia, sourcePi.GetValue(media, null), null);
        }

        private void _animationDirectoryMediaRemoved(object sender, MediaEventArgs e)
        {
            if (e.Media is AnimatedMedia media && AnimationDirectoryPRI is AnimationDirectory adirPri && AnimationDirectorySEC is AnimationDirectory adirSec && adirPri != adirSec)
                adirSec.FindMediaByMediaGuid(media.MediaGuid)?.Delete();
        }

        private void _animationDirectoryMediaAdded(object sender, MediaEventArgs e)
        {
            if (!(e.Media is AnimatedMedia media
                  && AnimationDirectoryPRI is AnimationDirectory adirPri
                  && AnimationDirectorySEC is AnimationDirectory adirSec
                  && adirPri != adirSec))
                return;
            var compMedia = adirSec.FindMediaByMediaGuid(media.MediaGuid);
            if (compMedia == null)
                adirSec.CloneMedia(media, media.MediaGuid);
        }

        private void _serverDirectoryMediaSaved(object dir, MediaEventArgs e)
        {
            if (!(e.Media is ServerMedia priMedia))
                throw new ApplicationException("Invalid media type provided");
            if (priMedia.MediaStatus == TMediaStatus.Deleted)
                return;
            var compMedia = _findComplementaryMedia(priMedia);
            if (compMedia?.IsModified == true && compMedia.IdPersistentMedia > 0)
                compMedia.Save();
        }

        private void _recorder_CaptureSuccess(object sender, MediaEventArgs e)
        {
            if (!(sender is CasparRecorder recorder))
                return;
            if ((recorder.RecordingDirectory == MediaDirectorySEC || recorder.RecordingDirectory != MediaDirectoryPRV) && recorder.RecordingDirectory != MediaDirectoryPRI)
                CopyMediaToPlayout(new[] { e.Media });
        }

        private MediaDeleteResult _deleteMedia(IMedia media, bool forceDelete)
        {
            if (forceDelete)
            {
                _fileManager.Queue(new DeleteOperation(_fileManager) { Source = media });
                return MediaDeleteResult.NoDeny;
            }
            else
            {
                var reason = media is PersistentMedia pm ? _engine.CanDeleteMedia(pm) : MediaDeleteResult.NoDeny;
                if (reason.Result == MediaDeleteResult.MediaDeleteResultEnum.Success)
                    _fileManager.Queue(new DeleteOperation(_fileManager) { Source = media });
                return reason;
            }
        }

        private ServerMedia _findComplementaryMedia(ServerMedia originalMedia)
        {
            var chPri = (CasparServerChannel)_engine.PlayoutChannelPRI;
            var chSec = (CasparServerChannel)_engine.PlayoutChannelSEC;
            if (chPri != null && chSec != null && chPri.Owner!= chSec.Owner)
            {
                if ((originalMedia.Directory as ServerDirectory)?.Server == chPri.Owner)
                    return (ServerMedia)((WatcherDirectory)chSec.Owner.MediaDirectory).FindMediaByMediaGuid(originalMedia.MediaGuid);
                if ((originalMedia.Directory as ServerDirectory)?.Server == chSec.Owner)
                    return (ServerMedia)((WatcherDirectory)chPri.Owner.MediaDirectory).FindMediaByMediaGuid(originalMedia.MediaGuid);
            }
            return null;
        }
        
        private void _export(MediaExportDescription export, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat)
        {
            _fileManager.Queue(new ExportOperation(_fileManager) { Sources = new[] { export }, DestProperties = export.Media, StartTC = export.StartTC, Duration = export.Duration, AudioVolume = export.AudioVolume, DestDirectory = directory as IngestDirectory, MXFAudioExportFormat = mXFAudioExportFormat, MXFVideoExportFormat = mXFVideoExportFormat });
        }

        private void _mediaPRIVerified(object o, MediaEventArgs e)
        {
            if (e.Media.MediaStatus != TMediaStatus.Available) return;
            if (!(MediaDirectorySEC is ServerDirectory sec) 
                || !(MediaDirectoryPRI is ServerDirectory pri) 
                || sec == pri 
                || !sec.IsInitialized 
                || !pri.IsInitialized)
                return;
            _engine.NotifyMediaVerified(e);
            var sEcMedia = sec.FindMediaByMediaGuid(e.Media.MediaGuid);
            if (sEcMedia != null)
                return;
            sEcMedia = sec.FindMediaFirst(sm => e.Media.FileSize == sm.FileSize && e.Media.FileName == sm.FileName && sm.FileExists()) as MediaBase;
            if (sEcMedia == null)
                FileManager.Queue(new CopyOperation(_fileManager) { Source = e.Media, DestDirectory = sec });
            else
            {
                sEcMedia.CloneMediaProperties(e.Media);
                sec.UpdateMediaGuid(sEcMedia, e.Media.MediaGuid);
                sEcMedia.MediaStatus = TMediaStatus.Unknown;
                sEcMedia.IsVerified = false;
                ThreadPool.QueueUserWorkItem(s => sEcMedia.Verify(false));
            }
        }

        private async void InitialMediaSynchronization()
        {
            if (!(MediaDirectoryPRI is ServerDirectory pri) || !(MediaDirectorySEC is ServerDirectory sec) ||
                pri == sec || !pri.IsInitialized || !sec.IsInitialized)
                return;
            if (Interlocked.Exchange(ref _isInitialMediaSecToPriSynchronized, 1) != default(int))
                return;
            try
            {
                await Task.Run(() => CopyMissingMediaPriToSec(pri, sec));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void CopyMissingMediaPriToSec(ServerDirectory pri, ServerDirectory sec)
        {
            Logger.Debug("SynchronizeMediaSecToPri started");
            var pRiMediaList = pri.GetAllFiles();
            foreach (var pRImedia in pRiMediaList)
            {
                if (pRImedia.MediaStatus != TMediaStatus.Available || !pRImedia.FileExists())
                    continue;
                var secMedia = sec.FindMediaByMediaGuid(pRImedia.MediaGuid);
                if (secMedia != null)
                    continue;
                secMedia = (ServerMedia) sec.FindMediaFirst(m =>
                    m.FileExists() 
                    && m.FileSize == pRImedia.FileSize 
                    && m.FileName == pRImedia.FileName 
                    && m.LastUpdated.DateTimeEqualToDays(pRImedia.LastUpdated)
                    && (!pri.IsRecursive || !sec.IsRecursive || string.Equals(pRImedia.Folder, m.Folder, StringComparison.OrdinalIgnoreCase))
                    );
                if (secMedia != null)
                {
                    secMedia.CloneMediaProperties(pRImedia);
                    sec.UpdateMediaGuid(secMedia, pRImedia.MediaGuid);
                    secMedia.Verify(false);
                }
                else
                    _fileManager.Queue(new CopyOperation(_fileManager)
                    {
                        Source = pRImedia,
                        DestDirectory = sec
                    });
            }
        }

        private void DeleteExtraSecMedia(ServerDirectory pri, ServerDirectory sec)
        {
            var secMediaList = sec.GetAllFiles();
            foreach (var secMedia in secMediaList)
            {
                if (pri.FindMediaByMediaGuid(secMedia.MediaGuid) == null)
                    _fileManager.Queue(new DeleteOperation(_fileManager)
                    {
                        Source = secMedia
                    });
            }
            var duplicatesList = secMediaList
                .Where(m => secMediaList.FirstOrDefault(d =>
                                d.MediaGuid == m.MediaGuid && ((ServerMedia)d).IdPersistentMedia !=
                                ((ServerMedia)m).IdPersistentMedia) != null)
                .Select(m => m.MediaGuid)
                .Distinct();
            foreach (var mediaGuid in duplicatesList)
                sec.FindMediaList(m => m.MediaGuid == mediaGuid)
                    .Skip(1).ToList()
                    .ForEach(m => m.Delete());
        }

        private void _mediaPRIRemoved(object o, MediaEventArgs e)
        {
            if (MediaDirectorySEC != null
                && MediaDirectorySEC != MediaDirectoryPRI
                && MediaDirectorySEC.IsInitialized)
            {
                var mediaToDelete = ((WatcherDirectory)MediaDirectorySEC).FindMediaByMediaGuid(e.Media.MediaGuid);
                if (mediaToDelete != null && mediaToDelete.FileExists())
                {
                    var operation = new DeleteOperation(_fileManager) {Source = mediaToDelete};
                    operation.Success += (sender, args) =>
                    {
                        foreach (var ingestDirectory in IngestDirectories)
                        {
                            if (((IngestDirectory) ingestDirectory).FindMediaByMediaGuid(operation.Source.MediaGuid) is
                                IngestMedia media)
                                media.IngestStatus = TIngestStatus.Unknown;
                        }
                    };
                    FileManager.Queue(operation);
                }
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();

            if (MediaDirectoryPRI is ServerDirectory sdir)
            {
                sdir.MediaPropertyChanged -= _serverMediaPropertyChanged;
                sdir.PropertyChanged -= _serverDirectoryPropertyChanged;
                sdir.MediaSaved -= _serverDirectoryMediaSaved;
                sdir.MediaVerified -= _mediaPRIVerified;
                sdir.MediaRemoved -= _mediaPRIRemoved;
            }
            sdir = MediaDirectorySEC as ServerDirectory;
            if (MediaDirectoryPRI != MediaDirectorySEC && sdir != null)
            {
                sdir.MediaPropertyChanged -= _serverMediaPropertyChanged;
                sdir.MediaSaved -= _serverDirectoryMediaSaved;
                sdir.PropertyChanged -= _serverDirectoryPropertyChanged;
            }
            if (AnimationDirectoryPRI is AnimationDirectory adir)
            {
                adir.PropertyChanged -= _animationDirectoryPropertyChanged;
                adir.MediaAdded -= _animationDirectoryMediaAdded;
                adir.MediaRemoved -= _animationDirectoryMediaRemoved;
                adir.MediaPropertyChanged -= _animationDirectoryMediaPropertyChanged;
                adir.MediaSaved -= _animationDirectoryMediaSaved;
            }
            adir = AnimationDirectorySEC as AnimationDirectory;
            if (adir != null)
                adir.PropertyChanged -= _animationDirectoryPropertyChanged;
            UnloadIngestDirs();
        }

    }


}
