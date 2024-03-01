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
using jNet.RPC;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server.Media
{
    public class IngestDirectory : WatcherDirectory, IIngestDirectory
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<string> _bMdXmlFiles = new List<string>();
        private int _xdcamClipCount;
        private NetworkCredential _networkCredential;
        private FtpClient _ftpClient;
        private bool _isRefreshing;

        private const string XmlFileExtension = ".xml";

        internal readonly object XdcamLockObject = new object();

        public bool DeleteSource { get; set; }

        public override async Task Initialize()
        {
            if (!string.IsNullOrWhiteSpace(Folder))
            {
                if (Folder.StartsWith("ftp://"))
                {
                    AccessType = TDirectoryAccessType.FTP;
                    IsInitialized = true;
                }
                else if (Kind == TIngestDirectoryKind.XDCAM || Kind == TIngestDirectoryKind.SimpleFolder)
                {
                    if (ConnectDirectory())
                    {
                        RefreshVolumeInfo();
                        IsInitialized = true;
                    }
                }
                else
                {
                    HaveFileWatcher = true;
                    if (IsImport && ConnectDirectory())
                    {
                        await BeginWatch();
                        IsInitialized = true;
                    }
                }
            }
            Parallel.ForEach(_subDirectories?.ToList(), async d => await d.Initialize());
        }

        public string EncodeParams { get; set; }

        public TMovieContainerFormat ExportContainerFormat { get; set; }

        public TVideoFormat ExportVideoFormat { get; set; }

        [DtoMember]
        public string DirectoryName { get; set; }

        [DtoMember]
        public TIngestDirectoryKind Kind { get; set; } = TIngestDirectoryKind.WatchFolder;

        [DtoMember]
        public bool IsWAN { get; set; }

        [XmlIgnore]
        [DtoMember]
        public int XdcamClipCount { get => _xdcamClipCount; protected set => SetField(ref _xdcamClipCount, value); }

        [DtoMember]
        public bool IsExport { get; set; }

        [DtoMember]
        public TVideoCodec VideoCodec { get; set; }

        [DtoMember]
        public TAudioCodec AudioCodec { get; set; }

        public double VideoBitrateRatio { get; set; } = 1;
        public double AudioBitrateRatio { get; set; } = 1;

        [DefaultValue(true)]
        [DtoMember]
        public bool IsImport { get; set; } = true;

        [DtoMember]
        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }

        [DtoMember]
        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }

        public string ExportParams { get; set; }

        [DtoMember]
        public bool MediaDoNotArchive { get; set; }

        [DtoMember]
        public int MediaRetnentionDays { get; set; }

        [DtoMember]
        public bool MediaLoudnessCheckAfterIngest { get; set; }

        [DtoMember]
        public TMediaCategory MediaCategory { get; set; }

        [DtoMember]
        public double AudioVolume { get; set; }

        [DtoMember]
        public TFieldOrder SourceFieldOrder { get; set; }

        [DtoMember]
        public TAspectConversion AspectConversion { get; set; }
        
        [XmlIgnore]
        [DtoMember]
        public TDirectoryAccessType AccessType { get; protected set; }

        [XmlArray(nameof(SubDirectories))]
        public List<IngestDirectory> _subDirectories;

        [XmlIgnore]
        [DtoMember]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories => _subDirectories;

        public string Username { get; set; }

        public string Password { get; set; }

        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }

        [DtoMember]
        [XmlIgnore]
        public override char PathSeparator => AccessType == TDirectoryAccessType.Direct ? Path.DirectorySeparatorChar : '/';

        public override void Refresh()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                if (AccessType == TDirectoryAccessType.Direct)
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

        public override IReadOnlyCollection<IMedia> GetAllFiles()
        {
            if (AccessType == TDirectoryAccessType.Direct && Kind != TIngestDirectoryKind.XDCAM)
                return base.GetAllFiles();
            if (Kind == TIngestDirectoryKind.XDCAM)
            {

                var client = AccessType == TDirectoryAccessType.FTP ? GetFtpClient() : null;
                client?.Connect();
                try
                {
                    return SearchInXdcam(client, string.Empty).ToList();
                }
                finally
                {
                    client?.Disconnect();
                }
            }
            return SearchFtp(string.Empty).ToList();
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
        
        internal override IMedia CreateMedia(IMediaProperties media)
        {
            throw new InvalidOperationException();
        }

        internal override bool DeleteMedia(IMedia media)
        {
            if (AccessType != TDirectoryAccessType.FTP)
                return base.DeleteMedia(media);
            if (!(media is MediaBase mediaBase) || mediaBase.Directory != this)
                throw new ApplicationException("Media does not belong to the directory");
            var client = GetFtpClient();
            var uri = new Uri(mediaBase.FullPath);
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
            if (AccessType == TDirectoryAccessType.Direct && Kind != TIngestDirectoryKind.XDCAM &&
                !string.IsNullOrWhiteSpace(Username))
            {
                var folder = Path.GetPathRoot(Folder);
                var user = PinvokeWindowsNetworking.GetConnectionUserName(folder);
                if (!string.IsNullOrWhiteSpace(user))
                    PinvokeWindowsNetworking.DisconnectRemote(folder);
            }
            _ftpClient?.Dispose();
        }

        protected override IMedia AddMediaFromPath(string fullPath, DateTime lastUpdated)
        {
            IngestMedia media;
            if (Kind == TIngestDirectoryKind.XDCAM)
            {
                throw new InvalidOperationException();
            }
            else
            {
                var relativeName = fullPath.Substring(Folder.Length);
                var fileName = Path.GetFileName(fullPath);
                var mediaType = FileUtils.GetMediaType(fileName);
                var isVerified = AccessType == TDirectoryAccessType.FTP && Kind != TIngestDirectoryKind.XDCAM;
                media = new IngestMedia
                {
                    MediaName = FileUtils.GetFileNameWithoutExtension(fileName, mediaType),
                    MediaGuid = Guid.NewGuid(),
                    MediaType = mediaType,
                    FileName = fileName,
                    LastUpdated = File.GetLastWriteTimeUtc(fullPath),
                    Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator),
                    MediaStatus = isVerified ? TMediaStatus.Available : TMediaStatus.Unknown,
                    IsVerified = isVerified
                };
            }
            AddMedia(media);
            return media;
        }

        private IMedia AddMediaFromFtp(string fullPath, FtpListItem item)
        {
            var relativeName = fullPath.Substring(Folder.Length);
            var fileName = Path.GetFileName(fullPath);
            var mediaType = FileUtils.GetMediaType(fileName);
            var isVerified = AccessType == TDirectoryAccessType.FTP && Kind != TIngestDirectoryKind.XDCAM;
            var media = new IngestMedia
            {
                MediaName = FileUtils.GetFileNameWithoutExtension(fileName, mediaType),
                MediaGuid = Guid.NewGuid(),
                MediaType = mediaType,
                Duration = item.Type == FtpFileSystemObjectType.Movie ? item.Size.SmpteFramesToTimeSpan("50") : TimeSpan.Zero,
                DurationPlay = item.Type == FtpFileSystemObjectType.Movie ? item.Size.SmpteFramesToTimeSpan("50") : TimeSpan.Zero,
                FileName = fileName,
                LastUpdated = item.Modified == default(DateTime) ? item.Created : item.Modified,
                Folder = relativeName.Substring(0, relativeName.Length - fileName.Length).Trim(PathSeparator),
                MediaStatus = isVerified ? TMediaStatus.Available : TMediaStatus.Unknown,
                IsVerified = isVerified
            };
            AddMedia(media);
            return media;
        }

        public override void AddMedia(IMedia media)
        {
            if (!(media is MediaBase mediaBase))
                throw new ArgumentException(nameof(media));
            if (!HaveFileWatcher)
            {
                mediaBase.Directory = this;
                return;
            }
            base.AddMedia(media);
        }
        
        public override IMediaSearchProvider Search(TMediaCategory? category, string searchString)
        {
            if (HaveFileWatcher)
                return base.Search(category, searchString);
            return new MediaSearchProvider(SearchForMediaForProvider(category, Folder, searchString));
        }

        protected override void OnError(object source, ErrorEventArgs e)
        {
            ClearFiles();
            base.OnError(source, e);
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

        protected override void EnumerateFiles(string directory)
        {
            base.EnumerateFiles(directory);
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
                   || (Kind == TIngestDirectoryKind.XDCAM && ext ==XDCAM.Smil.FileExtension);
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
                        {
                            xMlDoc.Load(stream);
                        }
                    }
                    catch (FtpCommandException e)
                    {
                        Logger.Error(e);
                        throw;
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
                        Credentials = GetNetworkCredential(),
                        ReadTimeout = 30000
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
                        m.TcStart = clip.SelectSingleNode(@"file/timecode/string").InnerText.SmpteTimecodeToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                        m.TcPlay = m.TcStart;
                        m.Duration = long.Parse(clip.SelectSingleNode(@"duration").InnerText).SmpteFramesToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                        m.DurationPlay = m.Duration;
                        m.BmdXmlFile = xmlFileName;
                    }
                }
                catch (NullReferenceException) { }
                catch (ArgumentNullException) { }
            }
            catch { }
        }

        private bool ConnectDirectory()
        {
            if (string.IsNullOrWhiteSpace(Username))
                return true;
            var dir = Path.GetPathRoot(Folder);
            var userName = PinvokeWindowsNetworking.GetConnectionUserName(dir);
            if (userName == Username)
                return true;
            string ret;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                ret = PinvokeWindowsNetworking.DisconnectRemote(dir);
                if (ret != null)
                    Logger.Warn("Cannot disconnect remote {0}. Error was: {1}", dir, ret);
            }
            ret = PinvokeWindowsNetworking.ConnectToRemote(dir, Username, Password);
            if (ret == null)
                return true;
            Logger.Warn("Cannot connect to remote {0}. Error was: {1}", dir, ret);
            return false;
        }


        private IEnumerable<IMedia> _ftpAddFileFromPath(FtpClient client, string rootPath, string localPath, FtpListItem item, string filter)
        {
            var newPath = localPath + '/' + item.Name;
            if ((item.Type == FtpFileSystemObjectType.Movie || item.Type == FtpFileSystemObjectType.File)
                && (string.IsNullOrEmpty(filter) || item.Name.ToLower().Contains(filter)))
                yield return AddMediaFromFtp(Folder + newPath, item);
            if (!IsRecursive || item.Type != FtpFileSystemObjectType.Directory)
                yield break;
            foreach (var file in client.GetListing(rootPath + newPath))
            foreach (var media in _ftpAddFileFromPath(client, rootPath, newPath, file, filter))
                yield return media;
        }

        private IEnumerable<IMedia> SearchFtp(string filter)
        {
            var ftpClient = GetFtpClient();
            var uri = new Uri(Folder, UriKind.Absolute);
            try
            {
                ftpClient.Connect();
                ClearFiles();
                foreach (var file in ftpClient.GetListing(uri.LocalPath))
                foreach (var media in _ftpAddFileFromPath(ftpClient, uri.LocalPath, "", file, filter))
                    yield return media;
            }
            finally
            {
                ftpClient.Disconnect();
            }
        }

        private IEnumerable<IMedia> SearchForMediaForProvider(TMediaCategory? category, string directory, string filter)
        {
            switch (Kind)
            {
                case TIngestDirectoryKind.XDCAM:
                    var client = AccessType == TDirectoryAccessType.FTP ? GetFtpClient() : null;
                    client?.Connect();
                    try
                    {
                        return SearchInXdcam(client, filter).ToList();
                    }
                    finally
                    {
                        client?.Disconnect();
                    }
                case TIngestDirectoryKind.SimpleFolder:
                    if (AccessType == TDirectoryAccessType.FTP)
                        return SearchFtp(filter);
                    return SearchInDirectories(category, directory, filter);
                default:
                    throw new InvalidOperationException();
            }
        }

        private IEnumerable<IMedia> SearchInDirectories(TMediaCategory? category, string directory, string filter)
        {
            var files = new DirectoryInfo(directory).EnumerateFiles(string.IsNullOrWhiteSpace(filter) ? "*" : $"*{filter}*");
            foreach (var f in files)
            {
                if (!AcceptFile(f.FullName))
                    continue;
                var m = AddMediaFromPath(f.FullName, f.LastWriteTimeUtc);
                if (m == null)
                    continue;
                if (category.HasValue && m.MediaCategory != category)
                    continue;
                yield return m;
            }
            if (!IsRecursive) yield break;
            var directories = new DirectoryInfo(directory).EnumerateDirectories();
            foreach (var d in directories)
            foreach (var m in SearchForMediaForProvider(category, d.FullName, filter))
                yield return m;
        }

        private IEnumerable<IMedia> SearchInXdcam(FtpClient client, string filter)
        {
            var mediaProfile =
                XDCAM.SerializationHelper<XDCAM.MediaProfile>.Deserialize(ReadXmlDocument("MEDIAPRO.XML", client));
            if (mediaProfile == null)
                yield break;
            XdcamClipCount = mediaProfile.Contents?.Length ?? 0;
            if (mediaProfile.Contents == null)
                yield break;
            var index = 0;
            foreach (var material in mediaProfile.Contents)
            {
                var format = TVideoFormat.Other;
                switch (material.videoType)
                {
                    case XDCAM.VideoType.Dv411Cbr25:
                    case XDCAM.VideoType.Dv420Cbr25:
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
                    case XDCAM.VideoType.Hd1920X1080Cbr50:
                    case XDCAM.VideoType.Hd1920X1080Vbr35:
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
                var duration = ((long)material.dur).SmpteFramesToTimeSpan(format);
                var fileName = material.uri?.ToLower();
                if (string.IsNullOrWhiteSpace(fileName) || !(string.IsNullOrWhiteSpace(filter) || fileName.Contains(filter)))
                    continue;
                var newMedia = new XDCAM.XdcamMedia
                {
                    MediaType = TMediaType.Movie,
                    MediaGuid = new Guid(material.umid.Substring(32)),
                    ClipNr = ++index,
                    FileName = Path.GetFileName(fileName),
                    MediaName = Path.GetFileNameWithoutExtension(fileName),
                    XdcamMaterial = material,
                    VideoFormat = format,
                    Duration = duration,
                    DurationPlay = duration
                };
                AddMedia(newMedia);
                yield return newMedia;
            }
        }

        #endregion

        public override string ToString()
        {
            return DirectoryName;
        }
    }

}
