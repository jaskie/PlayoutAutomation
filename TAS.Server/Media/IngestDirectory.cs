using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.XDCAM;

namespace TAS.Server.Media
{
    public class IngestDirectory : WatcherDirectory, IIngestDirectory
    {
        private readonly List<string> _bMdXmlFiles = new List<string>();
        private string _filter;
        private int _xdcamClipCount;
        private NetworkCredential _networkCredential;
        private FtpClient _ftpClient;
        private bool _isRefreshing;

        private const string XmlFileExtension = ".xml";

        internal readonly object XdcamLockObject = new object();

        internal IngestDirectory() : base(null)
        {
            IsImport = true;
            AudioBitrateRatio = 1;
            VideoBitrateRatio = 1;
        }

        public bool DeleteSource { get; set; }

        public override void Initialize()
        {
            if (Folder.StartsWith("ftp://"))
            {
                AccessType = TDirectoryAccessType.FTP;
                IsInitialized = true;
            }
            else
            if (Kind == TIngestDirectoryKind.XDCAM || IsWAN)
            {
                IsInitialized = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Username)
                    || _connectToRemoteDirectory())
                    if (IsImport && (!IsWAN || !string.IsNullOrWhiteSpace(_filter)))
                        BeginWatch(_filter, IsRecursive, TimeSpan.Zero);
            }
            _subDirectories?.ToList().ForEach(d =>
            {
                d.MediaManager = MediaManager;
                d.Initialize();
            });
        }

        public string EncodeParams { get; set; }

        public TMovieContainerFormat ExportContainerFormat { get; set; }

        public TVideoFormat ExportVideoFormat { get; set; }

        [JsonProperty]
        public TIngestDirectoryKind Kind { get; set; }

        [JsonProperty]
        public bool IsWAN { get; set; }

        [XmlIgnore]
        [JsonProperty]
        public int XdcamClipCount { get => _xdcamClipCount; protected set => SetField(ref _xdcamClipCount, value); }

        [JsonProperty]
        public bool IsRecursive { get; set; }

        [JsonProperty]
        public bool IsExport { get; set; }

        [JsonProperty]
        public TVideoCodec VideoCodec { get; set; }
        [JsonProperty]
        public TAudioCodec AudioCodec { get; set; }

        public double VideoBitrateRatio { get; set; }
        public double AudioBitrateRatio { get; set; }
        
        [DefaultValue(true)]
        [JsonProperty]
        public bool IsImport { get; set; }

        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }

        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }

        public string ExportParams { get; set; }

        [JsonProperty]
        public bool MediaDoNotArchive { get; set; }

        [JsonProperty]
        public int MediaRetnentionDays { get; set; }

        [JsonProperty]
        public bool MediaLoudnessCheckAfterIngest { get; set; }

        [JsonProperty]
        public TMediaCategory MediaCategory { get; set; }

        [JsonProperty]
        public double AudioVolume { get; set; }

        [JsonProperty]
        public TFieldOrder SourceFieldOrder { get; set; }

        [JsonProperty]
        public TAspectConversion AspectConversion { get; set; }

        [JsonProperty]
        public string Filter
        {
            get => _filter;
            set
            {
                if (!value.Equals(_filter))
                {
                    _filter = value;
                    if (!IsWAN)
                        return;
                    CancelBeginWatch();
                    ClearFiles();
                    BeginWatch(value, IsRecursive, TimeSpan.FromSeconds(10));
                }
            }
        }

        [XmlIgnore]
        [JsonProperty]
        public TDirectoryAccessType AccessType { get; protected set; }

        [XmlArray(nameof(SubDirectories))]
        public List<IngestDirectory> _subDirectories;

        [XmlIgnore]
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Objects)]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories => _subDirectories;

        public string Username { get; set; }

        public string Password { get; set; }

        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }

        [JsonProperty]
        [XmlIgnore]
        public override char PathSeparator => AccessType == TDirectoryAccessType.Direct ? Path.DirectorySeparatorChar : '/';

        public override void Refresh()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                if (Kind == TIngestDirectoryKind.XDCAM)
                {
                    if (Monitor.TryEnter(XdcamLockObject, 1000))
                        try
                        {
                            if (AccessType == TDirectoryAccessType.FTP)
                            {
                                var client = GetFtpClient() as XdcamClient;
                                if (client == null)
                                    return;
                                client.Connect();
                                try
                                {
                                    //VolumeFreeSize = client.GetFreeDiscSpace();
                                    _readXDCAM(client);
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }
                            else
                                _readXDCAM(null);
                        }
                        finally
                        {
                            Monitor.Exit(XdcamLockObject);
                        }
                    else
                        throw new ApplicationException("Nie udało się uzyskać dostępu do XDCAM");
                }
                else if (AccessType == TDirectoryAccessType.FTP)
                    _ftpDirectoryList();
                else
                    base.Refresh();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            List<IMedia> staleMediaList = FindMediaList(m => currentDateTime > m.LastUpdated.Date + TimeSpan.FromDays(MediaRetnentionDays));
            foreach (IMedia m in staleMediaList)
                m.Delete();
        }

        public override void RemoveMedia(IMedia media)
        {
            base.RemoveMedia(media);
            // remove xmlfile if it was last media file
            var ingestMedia = media as IngestMedia;
            if (!string.IsNullOrEmpty(ingestMedia?.BmdXmlFile)
                && FindMediaFirst(f => (f as IngestMedia)?.BmdXmlFile == ingestMedia.BmdXmlFile) == null)
                try
                {
                    var xmlFile = ingestMedia.BmdXmlFile;
                    if (!string.IsNullOrWhiteSpace(xmlFile) && File.Exists(xmlFile))
                        File.Delete(xmlFile);
                }
                catch
                {
                    // ignored
                }
        }


        internal override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }

        internal override bool DeleteMedia(IMedia media)
        {
            if (AccessType != TDirectoryAccessType.FTP)
                return base.DeleteMedia(media);
            if (media.Directory != this)
                throw new ApplicationException("Media does not belong to the directory");
            var client = GetFtpClient();
            var uri = new Uri(((MediaBase)media).FullPath);
            try
            {
                client.DeleteFile(uri.LocalPath);
                return true;
            }
            catch (FtpCommandException)
            {
                return false;
            }
        }

        public override bool FileExists(string filename, string subfolder = null)
        {
            if (AccessType == TDirectoryAccessType.FTP)
            {
                var client = GetFtpClient();
                var uri = new Uri(Folder + (string.IsNullOrWhiteSpace(subfolder) ?  "/" : $"/{subfolder}/") + filename);
                try
                {
                    var fileExists = client.FileExists(uri.LocalPath);
                    client.Disconnect();
                    return fileExists;
                }
                catch (FtpCommandException)
                {
                    client?.Disconnect();
                    return false;
                }
            }
            else
                return base.FileExists(filename, subfolder);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            if (AccessType == TDirectoryAccessType.Direct &&  Kind != TIngestDirectoryKind.XDCAM && !string.IsNullOrWhiteSpace(Username))
                PinvokeWindowsNetworking.DisconnectRemote(Path.GetPathRoot(Folder));
            _ftpClient?.Dispose();
        }

        protected override IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated)
        {
            IngestMedia media;
            if (Kind == TIngestDirectoryKind.XDCAM)
            {
                throw new NotImplementedException();
            }
            else
            {
                var relativeName = fullPath.Substring(Folder.Length);
                var fileName = Path.GetFileName(fullPath);
                var mediaType = FileUtils.GetMediaType(fileName);
                media = new IngestMedia
                {
                    MediaName = FileUtils.GetFileNameWithoutExtension(fileName, mediaType),
                    MediaGuid = Guid.NewGuid(),
                    MediaType = mediaType,
                    FileName = fileName,
                    Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator),
                    MediaStatus = TMediaStatus.Unknown,
                };
            }
            AddMedia(media);
            return media;
        }

        protected override void OnError(object source, ErrorEventArgs e)
        {
            base.OnError(source, e);
            IsInitialized = false;
            ClearFiles();
            Initialize();
        }

        internal override void RefreshVolumeInfo()
        {
            if (AccessType == TDirectoryAccessType.FTP)
            {
                if (!(GetFtpClient() is XdcamClient client))
                    return;
                client.Connect();
                try
                {
                    VolumeFreeSize = client.GetFreeDiscSpace();
                }
                finally
                {
                    client.Disconnect();
                }
            }
            else
                base.RefreshVolumeInfo();
        }

        protected override void OnFileRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                if (Kind == TIngestDirectoryKind.BmdMediaExpressWatchFolder &&
                    Path.GetExtension(e.Name).ToLowerInvariant() == XmlFileExtension)
                {
                    lock (((IList) _bMdXmlFiles).SyncRoot)
                    {
                        string xf = _bMdXmlFiles.FirstOrDefault(s => s == e.OldFullPath);
                        if (xf != null)
                        {
                            _bMdXmlFiles.Remove(xf);
                            _bMdXmlFiles.Add(e.FullPath);
                            foreach (var fd in FindMediaList(
                                f => (f is IngestMedia) && (f as IngestMedia).BmdXmlFile == xf))
                                ((IngestMedia) fd).BmdXmlFile = e.FullPath;
                        }
                    }
                }
                else
                    base.OnFileRenamed(source, e);
            }
            catch
            {
                // ignored
            }
        }

        protected override void OnMediaRenamed(MediaBase media, string newFullPath)
        {
            Debug.WriteLine(newFullPath, "OnMediaRenamed");
            media.MediaName = FileUtils.GetFileNameWithoutExtension(newFullPath, media.MediaType);
        }

        protected override void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (Kind == TIngestDirectoryKind.BmdMediaExpressWatchFolder)
                lock (((IList) _bMdXmlFiles).SyncRoot)
                {
                    var extension = Path.GetExtension(e.Name);
                    if (extension != null && extension.ToLower() == XmlFileExtension &&
                        _bMdXmlFiles.Contains(e.FullPath))
                    {
                        _scanXML(e.FullPath);
                        return;
                    }
                }
            var m = FindMediaFirstByFullPath(e.FullPath);
            if (m != null)
                    m.IsVerified = false;
        }

        protected override void EnumerateFiles(string directory, string filter, bool includeSubdirectories,
            CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, filter, includeSubdirectories, cancelationToken);
            if (Kind == TIngestDirectoryKind.BmdMediaExpressWatchFolder)
                lock (((IList) _bMdXmlFiles).SyncRoot)
                    foreach (string xml in _bMdXmlFiles)
                        _scanXML(xml);
        }

        protected override bool AcceptFile(string fullPath)
        {
            var ext = Path.GetExtension(fullPath)?.ToLowerInvariant();
            return Extensions == null
                   || Extensions.Length == 0
                   || Extensions.Any(e => e == ext)
                   || (Kind == TIngestDirectoryKind.XDCAM && ext == Smil.FileExtension);
        }

        protected override void FileRemoved(string fullPath)
        {
            if (Path.GetExtension(fullPath)?.ToLowerInvariant() == XmlFileExtension)
            {
                lock (((IList)_bMdXmlFiles).SyncRoot)
                    _bMdXmlFiles.Remove(fullPath);
                foreach (var fd in FindMediaList(f => (f is IngestMedia) && (f as IngestMedia).BmdXmlFile == fullPath))
                    ((IngestMedia)fd).BmdXmlFile = string.Empty;
            }
            else
                base.FileRemoved(fullPath);
        }

        internal XmlDocument ReadXmlDocument(string documentName, FtpClient client)
        {
            var xMlDoc = new XmlDocument();
            switch (AccessType)
            {
                case TDirectoryAccessType.Direct:
                    var fileName = Path.Combine(Folder, documentName);
                    if (File.Exists(fileName))
                        xMlDoc.Load(fileName);
                    break;
                case TDirectoryAccessType.FTP:
                    try
                    {
                        using (var stream = client.OpenRead(documentName))
                            xMlDoc.Load(stream);
                    }
                    catch (FtpCommandException e)
                    {
                        Logger.Error(e);
                    }
                    break;
            }
            return xMlDoc;
        }

        internal NetworkCredential GetNetworkCredential()
        {
            return _networkCredential ?? (_networkCredential = new NetworkCredential(Username, Password));
        }

        internal FtpClient GetFtpClient()
        {
            if (_ftpClient == null)
            {
                var uri = new Uri(Folder, UriKind.Absolute);
                _ftpClient = Kind == TIngestDirectoryKind.XDCAM ?
                    new XdcamClient(uri)
                    {
                        Credentials = GetNetworkCredential()
                    }
                    : new FtpClient
                    {
                        Host = uri.Host,
                        Credentials = GetNetworkCredential()
                    };
            }
            return _ftpClient;
        }

        #region Utilities

        // parse files from BMD's MediaExpress
        private void _scanXML(string xmlFileName)
        {
            foreach (var fd in FindMediaList(f => f is IngestMedia && ((IngestMedia) f).BmdXmlFile == xmlFileName))
                ((IngestMedia)fd).BmdXmlFile = string.Empty;
            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlFileName);
                try
                {
                    var clips = doc.SelectNodes(@"/xmeml/clip|/xmeml/bin/children/clip");
                    if (clips == null)
                        return;
                    foreach (XmlNode clip in clips)
                    {
                        string fileName = Path.GetFileName((new Uri(Uri.UnescapeDataString(clip.SelectSingleNode(@"file/pathurl").InnerText))).LocalPath);
                        IngestMedia m = (IngestMedia)FindMediaFirst(f => f.FileName == fileName);
                        if (m == null)
                            continue;
                        m.TcStart = clip.SelectSingleNode(@"file/timecode/string").InnerText.SMPTETimecodeToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                        m.TcPlay = m.TcStart;
                        m.Duration = long.Parse(clip.SelectSingleNode(@"duration").InnerText).SMPTEFramesToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                        m.DurationPlay = m.Duration;
                        m.BmdXmlFile = xmlFileName;
                    }
                }
                catch (NullReferenceException) { }
                catch (ArgumentNullException) { }
            }
            catch { }
        }

        private bool _connectToRemoteDirectory()
        {
            string dir = Path.GetPathRoot(Folder);
            string ret = PinvokeWindowsNetworking.DisconnectRemote(dir);
            if (ret != null)
                Debug.WriteLine(ret, $"DisconnectRemote {dir}");
            ret = PinvokeWindowsNetworking.ConnectToRemote(dir, Username, Password);
            if (ret == null)
                return true;
            Logger.Warn("Cannot connect to remote {0}. Error was: {1}", dir, ret);
            Debug.WriteLine(ret, $"ConnectToRemote {dir}");
            return false;
        }

        private async void _readXDCAM(XdcamClient client)
        {
            try
            {
                await Task.Run(() =>
                {
                    var mediaProfile =
                        XDCAM.SerializationHelper<XDCAM.MediaProfile>.Deserialize(ReadXmlDocument("MEDIAPRO.XML",
                            client));
                    if (mediaProfile == null)
                        return;
                    ClearFiles();
                    XdcamClipCount = mediaProfile.Contents.Length;
                    var index = 0;
                    foreach (var material in mediaProfile.Contents)
                    {
                        var format = TVideoFormat.Other;
                        switch (material.videoType)
                        {
                            case XDCAM.VideoType.Dv25:
                            case XDCAM.VideoType.Imx30:
                            case XDCAM.VideoType.Imx40:
                            case XDCAM.VideoType.Imx50:
                                switch (material.fps)
                                {
                                    case XDCAM.Fps.Fps50I:
                                        format = material.aspectRatio == XDCAM.AspectRatio.Narrow
                                            ? TVideoFormat.PAL
                                            : TVideoFormat.PAL_FHA;
                                        break;
                                    case XDCAM.Fps.Fps5994I:
                                        format = material.aspectRatio == XDCAM.AspectRatio.Narrow
                                            ? TVideoFormat.NTSC
                                            : TVideoFormat.NTSC_FHA;
                                        break;
                                }
                                break;
                            case XDCAM.VideoType.Hd1080Cbr50:
                            case XDCAM.VideoType.Hd1080Cbr35:
                                switch (material.fps)
                                {
                                    case XDCAM.Fps.Fps5994I:
                                        format = TVideoFormat.HD1080i5994;
                                        break;
                                    case XDCAM.Fps.Fps50I:
                                        format = TVideoFormat.HD1080i5000;
                                        break;
                                    case XDCAM.Fps.Fps2997P:
                                        format = TVideoFormat.HD1080p2997;
                                        break;
                                    case XDCAM.Fps.Fps5994P:
                                        format = TVideoFormat.HD1080p5994;
                                        break;
                                    case XDCAM.Fps.Fps25P:
                                        format = TVideoFormat.HD1080p2500;
                                        break;
                                    case XDCAM.Fps.Fps50P:
                                        format = TVideoFormat.HD1080p5000;
                                        break;
                                }
                                break;
                        }
                        var duration = ((long) material.dur).SMPTEFramesToTimeSpan(format);
                        var newMedia = new XdcamMedia
                        {
                            MediaType = TMediaType.Movie,
                            MediaGuid = new Guid(material.umid.Substring(32)),
                            ClipNr = ++index,
                            MediaName = $"{material.uri}",
                            XdcamMaterial = material,
                            VideoFormat = format,
                            Duration = duration,
                            DurationPlay = duration
                        };
                        AddMedia(newMedia);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void _ftpAddFileFromPath(FtpClient client, string rootPath, string localPath, FtpListItem item)
        {
            string newPath = localPath + '/' + item.Name;
            if (item.Type == FtpFileSystemObjectType.Movie || item.Type == FtpFileSystemObjectType.File)
            {
                IMedia newmedia = AddMediaFromPath(Folder + newPath, item.Modified == default(DateTime) ? item.Created : item.Modified);
                if (item.Type == FtpFileSystemObjectType.Movie)
                {
                    newmedia.Duration = item.Size.SMPTEFramesToTimeSpan("50"); // assuming Grass Valley K2 PAL server
                    newmedia.DurationPlay = newmedia.Duration;
                }
            }
            if (IsRecursive && item.Type == FtpFileSystemObjectType.Directory)
                foreach (var file in client.GetListing(rootPath + newPath))
                    _ftpAddFileFromPath(client, rootPath, newPath, file);
        }
        
        private void _ftpDirectoryList()
        {
            try
            {
                FtpClient _ftpClient = GetFtpClient();
                Uri uri = new Uri(Folder, UriKind.Absolute);
                try
                {
                    _ftpClient.Connect();
                    ClearFiles();
                    foreach (var file in _ftpClient.GetListing(uri.LocalPath))
                        _ftpAddFileFromPath(_ftpClient, uri.LocalPath, "", file);
                }
                finally
                {
                    _ftpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, ex.Message);
            }
        }

        #endregion

        public List<IMedia> Search(TMediaCategory? category, string searchString)
        {
            throw new NotImplementedException();
        }
    }

}
