using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Net.FtpClient;
using System.Net;
using System.ComponentModel;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.XDCAM;

namespace TAS.Server
{
    public class IngestDirectory : MediaDirectory, IIngestDirectory
    {
        public IngestDirectory() : base(null)
        {
            IsImport = true;
            AudioBitrateRatio = 1.0M;
            VideoBitrateRatio = 1.0M;
        }

        private bool _deleteSource;
        public bool DeleteSource
        {
            get { return _deleteSource; }
            set { _deleteSource = value; }
        }

        public override void Initialize()
        {
            if (_folder.StartsWith("ftp://"))
            {
                AccessType = TDirectoryAccessType.FTP;
                IsInitialized = true;
            }
            else
                if (IsXDCAM)
            {
                IsInitialized = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Username)
                    || _connectToRemoteDirectory())
                    if (!IsWAN && IsImport)
                        BeginWatch("*", IsRecursive, TimeSpan.Zero);
                    else
                        IsInitialized = true;
            }
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            if (AccessType == TDirectoryAccessType.Direct && !IsXDCAM && string.IsNullOrWhiteSpace(Username))
                PinvokeWindowsNetworking.disconnectRemote(Path.GetPathRoot(Folder));
            if (_ftpClient != null)
                _ftpClient.Dispose();
        }

        private bool _connectToRemoteDirectory()
        {
            string dir = Path.GetPathRoot(Folder);
            string ret = PinvokeWindowsNetworking.disconnectRemote(dir);
            if (ret != null)
                Debug.WriteLine(ret, $"DisconnectRemote {dir}");
            ret = PinvokeWindowsNetworking.connectToRemote(dir, Username, Password);
            if (ret == null)
                return true;
            Debug.WriteLine(ret, $"ConnectToRemote {dir}");
            return false;
        }

        internal object XdcamLockObject = new object();
        
        public string EncodeParams { get; set; }

        public TMediaExportContainerFormat ExportContainerFormat { get; set; }

        public TVideoFormat ExportVideoFormat { get; set; }

        [JsonProperty]
        public bool IsXDCAM { get; set; }

        [JsonProperty]
        public bool IsWAN { get; set; }

        private int _xdcamClipCount;
        [XmlIgnore]
        [JsonProperty]
        public int XdcamClipCount { get { return _xdcamClipCount; } protected set { SetField(ref _xdcamClipCount, value, nameof(XdcamClipCount)); } }

        [JsonProperty]
        public bool IsRecursive { get; set; }

        [JsonProperty]
        public bool IsExport { get; set; }

        [JsonProperty]
        public TVideoCodec VideoCodec { get; set; }
        [JsonProperty]
        public TAudioCodec AudioCodec { get; set; }

        public decimal VideoBitrateRatio { get; set; }
        public decimal AudioBitrateRatio { get; set; }
        
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
        public decimal AudioVolume { get; set; }

        [JsonProperty]
        public TFieldOrder SourceFieldOrder { get; set; }

        [JsonProperty]
        public TAspectConversion AspectConversion { get; set; }

        protected string _filter;
        [JsonProperty]
        public string Filter
        {
            get { return _filter; }
            set
            {
                if (!value.Equals(_filter))
                {
                    _filter = value;
                    if (IsWAN)
                    {
                        CancelBeginWatch();
                        ClearFiles();
                        BeginWatch(value, IsRecursive, TimeSpan.FromSeconds(10));
                    }
                }
            }
        }

        [XmlIgnore]
        [JsonProperty]
        public TDirectoryAccessType AccessType { get; protected set; }

        [XmlArray(nameof(SubDirectories))]
        public List<IngestDirectory> _subDirectories;

        [XmlIgnore]
        [JsonProperty]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return _subDirectories; } }

        public string Username { get; set; }

        public string Password { get; set; }

        private NetworkCredential _networkCredential;
        internal NetworkCredential _getNetworkCredential()
        {
                if (_networkCredential == null)
                    _networkCredential = new NetworkCredential(Username, Password);
                return _networkCredential;
        }

        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }

        [JsonProperty]
        [XmlIgnore]
        public override char PathSeparator { get { return AccessType == TDirectoryAccessType.Direct ? Path.DirectorySeparatorChar: '/'; } }

        private void _ftpAddFileFromPath(FtpClient client, string rootPath, string localPath, FtpListItem item)
        {
            string newPath = localPath + '/' + item.Name;
            if (item.Type == FtpFileSystemObjectType.Movie || item.Type == FtpFileSystemObjectType.File)
            {
                IMedia newmedia = AddFile(_folder + newPath, item.Modified == default(DateTime) ? item.Created : item.Modified);
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

        private FtpClient _ftpClient;
        internal FtpClient GetFtpClient() {
            if (_ftpClient == null)
            {
                FtpClient newClient = IsXDCAM ?
                    new XdcamClient()
                    {
                        Host = new Uri(_folder, UriKind.Absolute).Host,
                        Credentials = _getNetworkCredential()
                    }
                    : new FtpClient()
                    {
                        Host = new Uri(_folder, UriKind.Absolute).Host,
                        Credentials = _getNetworkCredential()
                    };
                _ftpClient = newClient;
            }
            return _ftpClient;
        }

        private bool _ftpDirectoryList()
        {
            bool exists = true;
            try
            {
                FtpClient _ftpClient = GetFtpClient();
                Uri uri = new Uri(_folder, UriKind.Absolute);
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
                exists = false;
                Debug.WriteLine(ex, ex.Message);
            }
            return exists;
        }


        bool _isRefreshing;
        public override void Refresh()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                if (IsXDCAM)
                {
                    if (Monitor.TryEnter(XdcamLockObject, 1000))
                        try
                        {
                            if (AccessType == TDirectoryAccessType.FTP)
                            {
                                var client = GetFtpClient() as XdcamClient;
                                Uri uri = new Uri(_folder, UriKind.Absolute);
                                client.Connect();
                                try
                                {
                                    VolumeFreeSize = client.GetFreeDiscSpace();
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
                else
                    if (AccessType == TDirectoryAccessType.FTP)
                        _ftpDirectoryList();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            List<IMedia> StaleMediaList = FindMediaList(m => currentDateTime > m.LastUpdated.Date + TimeSpan.FromDays(MediaRetnentionDays));
            foreach (IMedia m in StaleMediaList)
                m.Delete();
        }

        internal XmlDocument ReadXMLDocument(string documentName, FtpClient client)
        {
            XmlDocument xMLDoc = new XmlDocument();
            if (AccessType == TDirectoryAccessType.Direct)
            {
                string fileName = Path.Combine(_folder, documentName);
                if (File.Exists(fileName))
                    xMLDoc.Load(fileName);
            }
            if (AccessType == TDirectoryAccessType.FTP)
            {
                try
                {
                    using (Stream stream = client.OpenRead(documentName))
                        xMLDoc.Load(stream);
                }
                catch (FtpCommandException)
                { }
            }
            Debug.WriteLineIf(xMLDoc == null, $"_readXMLDocument didn\'t read {documentName}");
            return xMLDoc;
        }

        private void _readXDCAM(XdcamClient client)
        {
            try
            {
                var xdcamIndex = XDCAM.SerializationHelper<XDCAM.Index>.Deserialize(ReadXMLDocument("INDEX.XML", client));
                if (xdcamIndex != null)
                {
                    ClearFiles();
                    XdcamClipCount = xdcamIndex.clipTable.clipTable.Count;
                    var xdcamAlias = XDCAM.SerializationHelper<XDCAM.Alias>.Deserialize(ReadXMLDocument("ALIAS.XML", client));
                    int index = 0;
                    foreach (XDCAM.Index.Clip clip in xdcamIndex.clipTable.clipTable.Where(c => c.playable))
                    {
                        var clipAlias = xdcamAlias == null ? null : xdcamAlias.clipTable.FirstOrDefault(a => a.clipId == clip.clipId);
                        var newMedia = AddFile(string.Join(this.PathSeparator.ToString(), _folder, "Clip", $"{(clipAlias != null? clipAlias.value : clip.clipId)}.MXF"), default(DateTime), new Guid(clip.umid.Substring(12))) as XDCAMMedia;
                        if (newMedia != null)
                        {
                            newMedia.ClipNr = ++index;
                            newMedia.MediaName = $"{clip.clipId}";
                            newMedia.MediaType = TMediaType.Movie;
                            newMedia.XdcamClip = clip;
                            newMedia.XdcamAlias = clipAlias;
                            newMedia.Duration = ((long)clip.dur).SMPTEFramesToTimeSpan(clip.fps);
                            newMedia.DurationPlay = newMedia.Duration;
                            if (clip.aspectRatio == "4:3")
                                newMedia.VideoFormat = TVideoFormat.PAL;
                            if (clip.aspectRatio == "16:9")
                                newMedia.VideoFormat = TVideoFormat.PAL_FHA;
                        }
                    }
                    index = 0;
                    if (xdcamIndex.editlistTable != null && xdcamIndex.editlistTable.editlistTable != null)
                        foreach (XDCAM.Index.EditList edl in xdcamIndex.editlistTable.editlistTable)
                        {
                            var edlAlias = xdcamAlias == null ? null : xdcamAlias.editlistTable.FirstOrDefault(a => a.editlistId == edl.editlistId);
                            var newMedia = AddFile(string.Join(this.PathSeparator.ToString(), _folder, "Edit", $"{(edlAlias != null? edlAlias.value : edl.editlistId)}.SMI"), default(DateTime), new Guid(edl.umid.Substring(12))) as XDCAMMedia;
                            if (newMedia != null)
                            {
                                newMedia.MediaName = $"{edl.editlistId}";
                                newMedia.MediaType = TMediaType.Movie;
                                newMedia.XdcamEdl = edl;
                                newMedia.XdcamAlias = edlAlias;
                                newMedia.MediaType = TMediaType.Movie;
                                newMedia.Duration = ((long)edl.dur).SMPTEFramesToTimeSpan(edl.fps);
                                newMedia.DurationPlay = newMedia.Duration;
                                if (edl.aspectRatio == "4:3")
                                    newMedia.VideoFormat = TVideoFormat.PAL;
                                if (edl.aspectRatio == "16:9")
                                    newMedia.VideoFormat = TVideoFormat.PAL_FHA;
                            }
                        }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private SynchronizedCollection<string> _bMDXmlFiles = new SynchronizedCollection<string>();

        protected override bool AcceptFile(string fullPath)
        {
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            return Extensions == null
                || Extensions.Length == 0
                || Extensions.Any(e => e == ext)
                || (IsXDCAM && ext == XDCAM.Smil.FileExtension);
        }

        protected override IMedia AddFile(string fullPath, DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            if (Path.GetExtension(fullPath).ToLowerInvariant() == ".xml")
            {
                _bMDXmlFiles.Add(fullPath);
                return null;
            }
            else
                return base.AddFile(fullPath, lastWriteTime, guid);
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid = default(Guid))
        {
            return IsXDCAM
                ?
                new XDCAMMedia(this, guid)
                {
                    FullPath = fullPath,
                    MediaStatus = TMediaStatus.Unknown,
                    MediaCategory = this.MediaCategory
                }
                :
                new IngestMedia(this, guid)
                {
                    FullPath = fullPath,
                    MediaStatus = TMediaStatus.Unknown,
                    MediaCategory = this.MediaCategory,
                };
        }

        protected override void FileRemoved(string fullPath)
        {
            if (Path.GetExtension(fullPath).ToLowerInvariant() == ".xml")
            {
                _bMDXmlFiles.Remove(fullPath);
                    foreach (Media fd in FindMediaList(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == fullPath))
                        ((IngestMedia)fd).XmlFile = string.Empty;
            }
            else
                base.FileRemoved(fullPath);
        }

        public override void MediaRemove(IMedia media)
        {
            base.MediaRemove(media);
            // remove xmlfile if it was last media file
            if (media is IngestMedia && (media as IngestMedia).XmlFile != string.Empty)
            {
                if (!_files.Values.Any(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == (media as IngestMedia).XmlFile))
                    try
                    {
                        string fn = (media as IngestMedia).XmlFile;
                        if (!string.IsNullOrWhiteSpace(fn) && File.Exists(fn))
                            File.Delete(fn);
                    }
                    catch { };
            }
        }

        protected override void OnFileRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                Debug.WriteLine(e.FullPath, "OnFileRenamed");
                if (Path.GetExtension(e.Name).ToLowerInvariant() == ".xml")
                {
                    string xf = _bMDXmlFiles.FirstOrDefault(s => s == e.OldFullPath);
                    if (xf != null)
                    {
                        _bMDXmlFiles.Remove(xf);
                        _bMDXmlFiles.Add(e.FullPath);
                        foreach (Media fd in FindMediaList(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == xf))
                            ((IngestMedia)fd).XmlFile = e.FullPath;
                    }
                }
                else
                    base.OnFileRenamed(source, e);
            }
            catch { }
        }

        protected override void OnMediaRenamed(Media media, string newFullPath)
        {
            Debug.WriteLine(newFullPath, "OnMediaRenamed");
            string ext = Path.GetExtension(newFullPath).ToLowerInvariant();
            media.MediaName = FileUtils.GetFileNameWithoutExtension(newFullPath, media.MediaType);
        }

        protected override void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.Name).ToLower() == ".xml" && _bMDXmlFiles.Contains(e.FullPath))
            {
                _scanXML(e.FullPath);
            }
            Media m = (Media)_files.Values.FirstOrDefault(f => e.FullPath == f.FullPath);
            if (m!=null)
            {
                if (m.IsVerified)
                    m.IsVerified = false;
            }
        }

        protected override void EnumerateFiles(string directory, string filter, bool includeSubdirectories, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(directory, filter, includeSubdirectories, cancelationToken);
            foreach (string xml in _bMDXmlFiles)
                _scanXML(xml);
        }

        // parse files from BMD's MediaExpress
        private void _scanXML(string xmlFileName)
        {
                foreach (Media fd in FindMediaList(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == xmlFileName))
                    ((IngestMedia)fd).XmlFile = string.Empty;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlFileName);
                try
                {
                    XmlNodeList clips = doc.SelectNodes(@"/xmeml/clip|/xmeml/bin/children/clip");
                    foreach (XmlNode clip in clips)
                    {
                        string fileName = Path.GetFileName((new Uri(Uri.UnescapeDataString(clip.SelectSingleNode(@"file/pathurl").InnerText))).LocalPath);
                        IngestMedia m = (IngestMedia)FindMediaFirst(f => f.FileName == fileName);
                        if (m != null)
                        {
                            m.TcStart = clip.SelectSingleNode(@"file/timecode/string").InnerText.SMPTETimecodeToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                            m.TcPlay = m.TcStart;
                            m.Duration = Int64.Parse(clip.SelectSingleNode(@"duration").InnerText).SMPTEFramesToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                            m.DurationPlay = m.Duration;
                            m.XmlFile = xmlFileName;
                        }
                    }
                }
                catch (NullReferenceException) { }
                catch (ArgumentNullException) { }
            }
            catch (Exception) { }
        }
                       
        public IngestMedia FindMedia(string clipName)
        {
            string clipNameLowered = clipName.ToLower();
            IngestMedia result = (IngestMedia)FindMediaFirst(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.FileName)).ToLower() == clipNameLowered);
            if (result == null & IsWAN)
                {
                    string[] files = Directory.GetFiles(this.Folder, string.Format("{0}.*", clipName));
                    if (files.Length > 0)
                    {
                        string fileName = files[0];
                        result = (IngestMedia)this.CreateMedia(fileName);
                        result.MediaName = Path.GetFileNameWithoutExtension(fileName);
                    }
                }
            return result;
        }

        protected override void OnError(object source, ErrorEventArgs e)
        {
            base.OnError(source, e);
            IsInitialized = false;
            ClearFiles();
            Initialize();
        }

        protected override void GetVolumeInfo()
        {
            if (AccessType == TDirectoryAccessType.FTP)
            {
                var client = GetFtpClient() as XdcamClient;
                if (client != null)
                {
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
            }
            else
                base.GetVolumeInfo();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteMedia(IMedia media)
        {
            if (AccessType == TDirectoryAccessType.FTP)
            {
                if (media.Directory == this)
                {
                    FtpClient client = GetFtpClient();
                    Uri uri = new Uri(((Media)media).FullPath);
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
                else
                    return false;
            }
            else
                return base.DeleteMedia(media);
        }

        public override bool FileExists(string filename, string subfolder = null)
        {
            if (AccessType == TDirectoryAccessType.FTP)
            {
                FtpClient client = GetFtpClient();
                Uri uri = new Uri(_folder + (string.IsNullOrWhiteSpace(subfolder) ? "/" : $"/{subfolder}/") + filename);
                try
                {
                    return client.FileExists(uri.LocalPath);
                }
                catch (FtpCommandException)
                {
                    return false;
                }
            }
            else
                return base.FileExists(filename, subfolder);
        }

    }

}
