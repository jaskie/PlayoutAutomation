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

namespace TAS.Server
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true), CallbackBehavior]
    public class MediaManager: Remoting.IMediaManager
    {
        public readonly Engine Engine;
        public ServerDirectory MediaDirectoryPGM { get; private set; }
        public ServerDirectory MediaDirectoryPRV { get; private set; }
        public AnimationDirectory AnimationDirectoryPGM { get; private set; }
        public AnimationDirectory AnimationDirectoryPRV { get; private set; }
        public ArchiveDirectory ArchiveDirectory { get; private set; }
        public readonly ObservableSynchronizedCollection<Template> Templates = new ObservableSynchronizedCollection<Template>();


        public MediaManager(Engine engine)
        {
            Engine = engine;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal void Initialize()
        {
            MediaDirectoryPGM = (Engine.PlayoutChannelPGM == null) ? null : Engine.PlayoutChannelPGM.OwnerServer.MediaDirectory;
            MediaDirectoryPRV = (Engine.PlayoutChannelPRV == null) ? null : Engine.PlayoutChannelPRV.OwnerServer.MediaDirectory;
            AnimationDirectoryPGM = (Engine.PlayoutChannelPGM == null) ? null : Engine.PlayoutChannelPGM.OwnerServer.AnimationDirectory;
            AnimationDirectoryPRV = (Engine.PlayoutChannelPRV == null) ? null : Engine.PlayoutChannelPRV.OwnerServer.AnimationDirectory;

            ArchiveDirectory = DatabaseConnector.LoadArchiveDirectory(Engine.idArchive);
            Debug.WriteLine(this, "Begin initializing");
            ServerDirectory sdir = MediaDirectoryPGM as ServerDirectory;
            if (sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
                sdir.MediaSaved += _onServerDirectoryMediaSaved;
            }
            sdir = MediaDirectoryPRV as ServerDirectory;
            if (MediaDirectoryPGM != MediaDirectoryPRV && sdir != null)
            {
                sdir.MediaPropertyChanged += ServerMediaPropertyChanged;
                sdir.PropertyChanged += _onServerDirectoryPropertyChanged;
            }
            AnimationDirectory adir = AnimationDirectoryPGM;
            if (adir != null)
                adir.PropertyChanged += _onAnimationDirectoryPropertyChanged;

            LoadIngestDirs(ConfigurationManager.AppSettings["IngestFolders"]);
            Debug.WriteLine(this, "End initializing");
        }

        private List<IngestDirectory> _ingestDirectories;
        public IEnumerable<IngestDirectory> IngestDirectories
        {
            get
            {
                lock (_ingestDirsSyncObject)
                    return _ingestDirectories.ToList();
            }
        }

        private bool _ingestDirectoriesLoaded = false;
        private object _ingestDirsSyncObject = new object();

        internal void ReloadIngestDirs()
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
                    _ingestDirectories = (List<IngestDirectory>)reader.Deserialize(file);
                    file.Close();
                }
                else _ingestDirectories = new List<IngestDirectory>();
                _ingestDirectoriesLoaded = true;
                foreach (IngestDirectory d in _ingestDirectories)
                    d.Initialize();
            }
        }

        private void ServerMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (media is ServerMedia
                && !string.IsNullOrEmpty(e.PropertyName)
                   && (e.PropertyName == "DoNotArchive"
                    || e.PropertyName == "HasExtraLines"
                    || e.PropertyName == "idAux"
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

        public List<MediaDirectory> Directories()
        {
            List<MediaDirectory> dl = new List<MediaDirectory>();
            lock (_ingestDirsSyncObject)
                if (_ingestDirectoriesLoaded)
                    foreach (IngestDirectory d in _ingestDirectories)
                        dl.Add(d);
            if (ArchiveDirectory != null)
                dl.Insert(0, ArchiveDirectory);
            if (Engine.PlayoutChannelPRV != null && Engine.PlayoutChannelPRV.OwnerServer != Engine.PlayoutChannelPGM.OwnerServer)
                dl.Insert(0, Engine.PlayoutChannelPRV.OwnerServer.MediaDirectory);
            if (Engine.PlayoutChannelPGM != null)
                dl.Insert(0, Engine.PlayoutChannelPGM.OwnerServer.MediaDirectory);
            return dl;
        }

        public void IngestMediaToPlayout(Media media, bool toTop = false)
        {
            if (media != null)
            {
                if (media is ServerMedia)
                {
                    if (media.MediaStatus == TMediaStatus.Deleted || media.MediaStatus == TMediaStatus.Required || media.MediaStatus == TMediaStatus.CopyError)
                    {
                        if (ArchiveDirectory != null)
                        {
                            ArchiveMedia fromArchive = ArchiveDirectory.Find(media);
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
                ServerMedia sm = null;
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

        public void IngestMediaToPlayout(IEnumerable<Media> mediaList, bool ToTop = false)
        {
            foreach (Media m in mediaList)
                IngestMediaToPlayout(m, ToTop);
        }

        public void IngestMediaToArchive(IngestMedia media, bool toTop = false)
        {
            if (media == null)
                return;
            if (ArchiveDirectory != null)
            {
                ArchiveMedia destMedia;
                destMedia = ArchiveDirectory.GetArchiveMedia(media);
                if (!destMedia.FileExists())
                    FileManager.Queue(new ConvertOperation { SourceMedia = media, DestMedia = destMedia, OutputFormat = Engine.VideoFormat }, toTop);
            }
        }

        public void IngestMediaToArchive(ArchiveMedia media, bool toTop = false)
        {
            if (media == null || ArchiveDirectory == null)
                return;
            Media sourceMedia = media.OriginalMedia;
            if (sourceMedia == null || !(sourceMedia is IngestMedia))
                return;
            if (!media.FileExists() && sourceMedia.FileExists())
                FileManager.Queue(new ConvertOperation { SourceMedia = sourceMedia, DestMedia = media, OutputFormat = Engine.VideoFormat }, toTop);
        }

        public void IngestMediaToArchive(IEnumerable<IngestMedia> mediaList, bool ToTop = false)
        {
            foreach (IngestMedia m in mediaList)
                IngestMediaToArchive(m);
        }

        public void DeleteMedia(Media media)
        {
            if (!(media is ServerMedia) || Engine.CanDeleteMedia(media as ServerMedia))
                FileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Delete, SourceMedia = media });
        }

        public void DeleteMedia(IEnumerable<Media> mediaList)
        {
            foreach (Media m in mediaList)
                DeleteMedia(m);
        }

        public void GetLoudness(Media media)
        {
            if (media.Directory.AccessType == TDirectoryAccessType.Direct)
                FileManager.Queue(new LoudnessOperation() { Kind = TFileOperationKind.Loudness, SourceMedia = media });
        }

        public void GetLoudness(IEnumerable<Media> mediaList)
        {
            foreach (Media m in mediaList)
                GetLoudness(m);
        }


        public bool CanDeleteMedia(Media mediaList)
        {
            if (mediaList is ServerMedia)
                return Engine.CanDeleteMedia(mediaList as ServerMedia);
            else
                return true;
        }
        
        public void ArchiveMedia(Media media, bool deleteAfter)
        {
            if (ArchiveDirectory == null)
                return;
            if (media is ServerMedia)
                ArchiveDirectory.ArchiveSave(media, Engine.VideoFormat, deleteAfter);
            if (media is IngestMedia)
                IngestMediaToArchive((IngestMedia)media, false);
            if (media is ArchiveMedia)
                IngestMediaToArchive((ArchiveMedia)media, false);
        }
        
        private ServerMedia _findComplementaryMedia(ServerMedia originalMedia)
        {
            if (Engine.PlayoutChannelPGM != null && Engine.PlayoutChannelPRV != null && Engine.PlayoutChannelPGM.OwnerServer != Engine.PlayoutChannelPRV.OwnerServer)
            {
                if ((originalMedia.Directory as ServerDirectory).Server == Engine.PlayoutChannelPGM.OwnerServer && Engine.PlayoutChannelPRV != null)
                    return (ServerMedia)Engine.PlayoutChannelPRV.OwnerServer.MediaDirectory.FindMedia(originalMedia);
                if ((originalMedia.Directory as ServerDirectory).Server == Engine.PlayoutChannelPRV.OwnerServer && Engine.PlayoutChannelPGM != null)
                    return (ServerMedia)Engine.PlayoutChannelPGM.OwnerServer.MediaDirectory.FindMedia(originalMedia);
            }
            return null;
        }

        private ServerMedia _getComplementaryMedia(ServerMedia originalMedia)
        {
            if (Engine.PlayoutChannelPGM != null && Engine.PlayoutChannelPRV != null && Engine.PlayoutChannelPGM.OwnerServer != Engine.PlayoutChannelPRV.OwnerServer)
            {
                if ((originalMedia.Directory as ServerDirectory).Server == Engine.PlayoutChannelPGM.OwnerServer && Engine.PlayoutChannelPRV != null)
                    return (ServerMedia)Engine.PlayoutChannelPRV.OwnerServer.MediaDirectory.GetServerMedia(originalMedia);
                if ((originalMedia.Directory as ServerDirectory).Server == Engine.PlayoutChannelPRV.OwnerServer && Engine.PlayoutChannelPGM != null)
                    return (ServerMedia)Engine.PlayoutChannelPGM.OwnerServer.MediaDirectory.GetServerMedia(originalMedia);
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
                        ServerMedia pRVmedia = (ServerMedia)MediaDirectoryPRV.FindMedia(pGMmedia);
                        if (pRVmedia == null)
                        {
                            pRVmedia = (ServerMedia)MediaDirectoryPRV.Files.FirstOrDefault((m) => m.FileExists() && m.FileSize == pGMmedia.FileSize && m.FileName == pGMmedia.FileName && m.LastUpdated.DateTimeEqualToDays(pGMmedia.LastUpdated)); 
                            if (pRVmedia != null)
                            {
                                pRVmedia.CloneMediaProperties(pGMmedia);;
                                pRVmedia.Save();
                            }
                            else
                            {
                                pRVmedia = MediaDirectoryPRV.GetServerMedia(pGMmedia, true);
                                FileManager.Queue(new FileOperation() { Kind = TFileOperationKind.Copy, SourceMedia = pGMmedia, DestMedia = pRVmedia });
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
            return Engine.EngineName + ":MediaManager";
        }


        public void Export(IEnumerable<MediaExport> exportList, IngestDirectory directory)
        {
            foreach (MediaExport e in exportList)
                Export(e, directory);

        }

        public void Export(MediaExport export, IngestDirectory directory)
        {
            FileManager.Queue(new XDCAM.ExportOperation() { SourceMedia = export.Media, StartTC = export.StartTC, Duration = export.Duration, AudioVolume = export.AudioVolume, DestDirectory = directory });
        }

        public Guid IngestFile(string fileName)
        {
            var nameLowered = fileName.ToLower();
            ServerMedia dest;
            if ((dest  = (ServerMedia)(MediaDirectoryPGM.FindMedia(m => Path.GetFileNameWithoutExtension(m.FileName).ToLower() == nameLowered).FirstOrDefault())) != null)
                return dest.MediaGuid;
            foreach (IngestDirectory dir in IngestDirectories)
            {
                Media source = dir.FindMedia(fileName);
                if (source != null)
                {
                    source.Verify();
                    if (source.MediaStatus == TMediaStatus.Available)
                    {
                        dest = MediaDirectoryPGM.GetServerMedia(source, false);
                        FileManager.Queue(new ConvertOperation()
                        {
                            SourceMedia = source,
                            DestMedia = dest,
                            OutputFormat = Engine.VideoFormat,
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


        #region Remote interface
        public Remoting.IMediaManagerCallback MediaManagerCallback;
        
        public void OpenSession()
        {
            MediaManagerCallback = OperationContext.Current.GetCallbackChannel<Remoting.IMediaManagerCallback>();
            Debug.WriteLine("Remote interface connected");
        }
        #endregion // Remote interface

    }


}
