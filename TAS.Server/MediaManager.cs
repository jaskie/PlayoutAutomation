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

    public class MediaManager : DtoBase, IMediaManager
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
        public ICGElementsController CGElementsController { get { return _engine.CGElementsController; } }

        //[JsonProperty]
        public VideoFormatDescription FormatDescription { get { return _engine.FormatDescription; } }
        [JsonProperty]
        public TVideoFormat VideoFormat { get { return _engine.VideoFormat; } }
        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(MediaManager));


        public MediaManager(Engine engine)
        {
            _engine = engine;
            _fileManager = new FileManager() { TempDirectory = new TempDirectory(this) };
        }

        public void Initialize()
        {
            Debug.WriteLine(this, "Begin initializing");
            Logger.Debug("Begin initializing");
            ArchiveDirectory = this.LoadArchiveDirectory<ArchiveDirectory>(_engine.IdArchive);
            MediaDirectoryPRI = (_engine.PlayoutChannelPRI == null) ? null : _engine.PlayoutChannelPRI.OwnerServer.MediaDirectory;
            MediaDirectorySEC = (_engine.PlayoutChannelSEC == null) ? null : _engine.PlayoutChannelSEC.OwnerServer.MediaDirectory;
            MediaDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.MediaDirectory;
            AnimationDirectoryPRI = (_engine.PlayoutChannelPRI == null) ? null : _engine.PlayoutChannelPRI.OwnerServer.AnimationDirectory;
            AnimationDirectorySEC = (_engine.PlayoutChannelSEC == null) ? null : _engine.PlayoutChannelSEC.OwnerServer.AnimationDirectory;
            AnimationDirectoryPRV = (_engine.PlayoutChannelPRV == null) ? null : _engine.PlayoutChannelPRV.OwnerServer.AnimationDirectory;
            IMediaDirectory[] initializationList = new IMediaDirectory[] { MediaDirectoryPRI, MediaDirectorySEC, MediaDirectoryPRV, AnimationDirectoryPRI, AnimationDirectorySEC, AnimationDirectoryPRV, ArchiveDirectory };
            foreach (IMediaDirectory dir in initializationList.OfType<IMediaDirectory>().Distinct())
                dir.Initialize();
            if (ArchiveDirectory != null)
                ArchiveDirectory.MediaDeleted += ArchiveDirectory_MediaDeleted;

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
            {
                adir.PropertyChanged += _onAnimationDirectoryPropertyChanged;
                adir.MediaAdded += _onAnimationDirectoryMediaAdded;
                adir.MediaRemoved += _onAnimationDirectoryMediaRemoved;
                adir.MediaPropertyChanged += _onAnimationDirectoryMediaPropertyChanged;
            }
            adir = AnimationDirectorySEC;
            if (adir != null)
                adir.PropertyChanged += _onAnimationDirectoryPropertyChanged;

            LoadIngestDirs(ConfigurationManager.AppSettings["IngestFolders"]);
            _fileManager.VolumeReferenceLoudness = Convert.ToDecimal(_engine.VolumeReferenceLoudness);
            Debug.WriteLine(this, "End initializing");
            Logger.Debug("End initializing");
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

        public IEnumerable<ICGElement> Parentals
        {
            get
            {
                return null;
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

        private void ServerMediaPropertyChanged(object dir, MediaPropertyChangedEventArgs e)
        {
            var adirPri = MediaDirectoryPRI;
            var adirSec = MediaDirectorySEC;
            if (e.Media is ServerMedia
                && (adirPri != null && adirSec != null && adirPri != adirSec)
                && !string.IsNullOrEmpty(e.PropertyName)
                   && (e.PropertyName == "DoNotArchive"
                    || e.PropertyName == "IdAux"
                    || e.PropertyName == "IdProgramme"
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
                    || e.PropertyName == "FieldOrderInverted"
                    ))
            {
                ServerMedia compMedia = _findComplementaryMedia(e.Media as ServerMedia);
                if (compMedia != null)
                {
                    if (e.PropertyName == "FileName")
                        compMedia.RenameTo(((ServerMedia)e.Media).FileName);
                    else
                    {
                        PropertyInfo sourcePi = e.Media.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = compMedia.GetType().GetProperty(e.PropertyName);
                        if (sourcePi != null && destPi != null)
                            destPi.SetValue(compMedia, sourcePi.GetValue(e.Media, null), null);
                    }
                }
            }
        }

        private void _onServerDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInitialized")
                SynchronizeMediaSecToPri(false);
        }

        #region AnimationDirectory event handlers

        private void _onAnimationDirectoryPropertyChanged(object dir, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsInitialized")
            {
                ThreadPool.QueueUserWorkItem((o) => SynchronizeAnimationsSecToPri());
            }
        }

        private void _onAnimationDirectoryMediaPropertyChanged(object o, MediaPropertyChangedEventArgs e)
        {
            var adirPri = AnimationDirectoryPRI as AnimationDirectory;
            var adirSec = AnimationDirectorySEC as AnimationDirectory;
            var media = e.Media as AnimatedMedia;
            if (media != null
                && (adirPri != null && adirSec != null && adirPri != adirSec)
                && !string.IsNullOrEmpty(e.PropertyName)
                && (e.PropertyName == nameof(IAnimatedMedia.MediaName)
                    || e.PropertyName == nameof(IAnimatedMedia.Fields)
                    || e.PropertyName == nameof(IAnimatedMedia.Method)
                    || e.PropertyName == nameof(IAnimatedMedia.TemplateLayer)
                ))
            {
                AnimatedMedia compMedia = adirSec.FindMediaByMediaGuid(media.MediaGuid) as AnimatedMedia;
                if (compMedia != null)
                {
                    PropertyInfo sourcePi = media.GetType().GetProperty(e.PropertyName);
                    PropertyInfo destPi = compMedia.GetType().GetProperty(e.PropertyName);
                    if (sourcePi != null && destPi != null)
                        destPi.SetValue(compMedia, sourcePi.GetValue(media, null), null);
                }
            }
        }


        private void _onAnimationDirectoryMediaRemoved(object sender, MediaEventArgs e)
        {
            var adirPri = AnimationDirectoryPRI as AnimationDirectory;
            var adirSec = AnimationDirectorySEC as AnimationDirectory;
            var media = e.Media as AnimatedMedia;
            if (media != null
                && (adirPri != null && adirSec != null && adirPri != adirSec))
                adirSec.FindMediaByMediaGuid(media.MediaGuid)?.Delete();
        }

        private void _onAnimationDirectoryMediaAdded(object sender, MediaEventArgs e)
        {
            var adirPri = AnimationDirectoryPRI as AnimationDirectory;
            var adirSec = AnimationDirectorySEC as AnimationDirectory;
            var media = e.Media as AnimatedMedia;
            if (media != null
                && (adirPri != null && adirSec != null && adirPri != adirSec))
            {
                var compMedia = adirSec.FindMediaByMediaGuid(media.MediaGuid);
                if (compMedia == null)
                    adirSec.CloneMedia(media, media.MediaGuid);
            }
        }


        #endregion //AnimationDirectory event handlers

        private void _onServerDirectoryMediaSaved(object dir, MediaEventArgs e)
        {
            ServerMedia priMedia = e.Media as ServerMedia;
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

        public void MeasureLoudness(IEnumerable<IMedia> mediaList)
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

        object _lockSynchronizeMediaSecToPri = new object();
        bool _isSynchronizedMediaSecToPri = false;
        public void SynchronizeMediaSecToPri(bool deleteNotExisted)
        {
            var pri = MediaDirectoryPRI as ServerDirectory;
            var sec = MediaDirectorySEC as ServerDirectory;
            if (pri != null && sec != null
                && pri != sec
                && pri.IsInitialized
                && sec.IsInitialized)
            {
                ThreadPool.QueueUserWorkItem(o =>
               {
                   lock (_lockSynchronizeMediaSecToPri)
                   {
                       if (!_isSynchronizedMediaSecToPri)
                       {
                           Debug.WriteLine(this, "SynchronizeMediaSecToPri started");
                           Logger.Debug("SynchronizeMediaSecToPri started");
                           var pRIMediaList = pri.GetFiles();
                           foreach (ServerMedia pRImedia in pRIMediaList)
                           {
                               if (pRImedia.MediaStatus == TMediaStatus.Available && pRImedia.FileExists())
                               {
                                   ServerMedia secMedia = (ServerMedia)sec.FindMediaByMediaGuid(pRImedia.MediaGuid);
                                   if (secMedia == null)
                                   {
                                       secMedia = (ServerMedia)sec.FindMediaFirst(m => m.FileExists() && m.FileSize == pRImedia.FileSize && m.FileName == pRImedia.FileName && m.LastUpdated.DateTimeEqualToDays(pRImedia.LastUpdated));
                                       if (secMedia != null)
                                       {
                                           secMedia.CloneMediaProperties(pRImedia);
                                           secMedia.MediaGuid = pRImedia.MediaGuid;
                                           secMedia.Verify();
                                       }
                                       else
                                       {
                                           secMedia = (ServerMedia)sec.GetServerMedia(pRImedia, true);
                                           _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Copy, SourceMedia = pRImedia, DestMedia = secMedia });
                                       }
                                   }
                               }
                           }
                           if (deleteNotExisted)
                           {
                               var secMediaList = sec.GetFiles().ToList();
                               foreach (ServerMedia secMedia in secMediaList)
                               {
                                   if ((ServerMedia)pri.FindMediaByMediaGuid(secMedia.MediaGuid) == null)
                                       _fileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Delete, SourceMedia = secMedia });
                               }
                               var duplicatesList = secMediaList.Where(m => secMediaList.FirstOrDefault(d => d.MediaGuid == m.MediaGuid && ((ServerMedia)d).IdPersistentMedia != ((ServerMedia)m).IdPersistentMedia) != null).Select(m => m.MediaGuid).Distinct();
                               foreach (var mediaGuid in duplicatesList)
                                   sec.FindMediaList(m => m.MediaGuid == mediaGuid)
                                   .Skip(1).ToList()
                                   .ForEach(m => m.Delete());
                           }
                       }
                       _isSynchronizedMediaSecToPri = true;
                       Logger.Debug("SynchronizeMediaSecToPri finished");
                   }
               });
            }
        }

        object _lockSynchronizeAnimationsSecToPri = new object();
        bool _isSynchronizedAnimationsSecToPri = false;

        public void SynchronizeAnimationsSecToPri()
        {
            var pri = AnimationDirectoryPRI as AnimationDirectory;
            var sec = AnimationDirectorySEC as AnimationDirectory;
            if (pri != null && sec != null
                && pri != sec
                && pri.IsInitialized
                && sec.IsInitialized)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    lock (_lockSynchronizeAnimationsSecToPri)
                    {
                        if (!_isSynchronizedAnimationsSecToPri)
                        {
                            Debug.WriteLine(this, "SynchronizeAnimationsSecToPri started");
                            Logger.Debug("SynchronizeAnimationsSecToPri started");
                            var priAnimations = pri.GetFiles().ToList();
                            foreach (AnimatedMedia priAnimation in priAnimations)
                            {
                                if (priAnimation.MediaStatus == TMediaStatus.Available)
                                {
                                    AnimatedMedia sECAnimation = (AnimatedMedia)((AnimationDirectory)sec).FindMediaByMediaGuid(priAnimation.MediaGuid);
                                    if (sECAnimation == null)
                                    {
                                        sECAnimation = (AnimatedMedia)((MediaDirectory)sec).FindMediaFirst(m => m.Folder == priAnimation.Folder && m.FileName == priAnimation.FileName && !priAnimations.Any(a => a.MediaGuid == m.MediaGuid));
                                        if (sECAnimation != null)
                                        {
                                            sECAnimation.CloneMediaProperties(priAnimation);
                                            sECAnimation.MediaGuid = priAnimation.MediaGuid;
                                            sECAnimation.Save();
                                            Debug.WriteLine(sECAnimation, "Updated");
                                        }
                                        else
                                        {
                                            var secFileName = Path.Combine(sec.Folder, priAnimation.Folder, priAnimation.FileName);
                                            if (File.Exists(secFileName))
                                                sec.CloneMedia(priAnimation, priAnimation.MediaGuid);
                                        }
                                    }
                                }
                            }
                            _isSynchronizedAnimationsSecToPri = true;
                            Logger.Debug("SynchronizeAnimationsSecToPri finished");
                        }
                    }
                });
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
            var sec = MediaDirectorySEC as ServerDirectory;
            var pri = MediaDirectoryPRI as ServerDirectory;
            if (sec != null && pri != null
                && sec != pri
                && sec.IsInitialized
                && pri.IsInitialized)
            {
                ServerMedia sECMedia = sec.FindMediaFirst(sm => e.Media.FileSize == sm.FileSize
                            && e.Media.FileName == sm.FileName && sm.FileExists()) as ServerMedia;
                if (e.Media.MediaStatus == TMediaStatus.Available)
                    if (sECMedia == null)
                        FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Copy, SourceMedia = e.Media, DestMedia = sec.GetServerMedia(e.Media, true) }, false);
                    else
                    {
                        sECMedia.CloneMediaProperties(e.Media);
                        sECMedia.MediaGuid = e.Media.MediaGuid;
                        sECMedia.ReVerify();
                    }
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
