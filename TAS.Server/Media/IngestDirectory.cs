using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using TAS.Common;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.Net.FtpClient;
using TAS.Server.Interfaces;
using Newtonsoft.Json;
using System.Net;

namespace TAS.Server
{
    public class IngestDirectory : MediaDirectory, IIngestDirectory
    {
        public IngestDirectory() : base(null) { }

        private bool _deleteSource;
        public bool DeleteSource
        {
            get { return _deleteSource; }
            set { _deleteSource = value; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Initialize()
        {
            if (_folder.StartsWith("ftp://"))
                AccessType = TDirectoryAccessType.FTP;
            else
                if (IsXDCAM)
            {
                Refresh();
                IsInitialized = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Username)
                    || _connectToRemoteDirectory())
                    if (!IsWAN)
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
        }

        private bool _connectToRemoteDirectory()
        {
            string dir = Path.GetPathRoot(Folder);
            string ret = PinvokeWindowsNetworking.disconnectRemote(dir);
            if (ret != null)
                Debug.WriteLine(ret, string.Format("DisconnectRemote {0}", dir));
            ret = PinvokeWindowsNetworking.connectToRemote(dir, Username, Password);
            if (ret == null)
                return true;
            Debug.WriteLine(ret, string.Format("ConnectToRemote {0}", dir));
            return false;
        }

        private object _xdcamLockObject = new object();
        
        public string EncodeParams { get; set; }

        [JsonProperty]
        public bool DoNotEncode { get; set; }
        
        [JsonProperty]
        public bool IsXDCAM { get; set; }

        [JsonProperty]
        public bool IsWAN { get; set; }

        [JsonProperty]
        public bool IsRecursive { get; set; }

        [JsonProperty]
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }

        [JsonProperty]
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }

        [JsonProperty]
        public bool MediaDoNotArchive { get; set; }

        [JsonProperty]
        public int MediaRetnentionDays { get; set; }

        [JsonProperty]
        public TMediaCategory MediaCategory { get; set; }

        [JsonProperty]
        public decimal AudioVolume { get; set; }

        [JsonProperty]
        public TFieldOrder SourceFieldOrder { get; set; }

        [JsonProperty]
        public TAspectConversion AspectConversion { get; set; }

        [JsonProperty]
        protected string _filter;
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

        public string Username { get; set; }

        public string Password { get; set; }

        private NetworkCredential _networkCredential;
        public NetworkCredential NetworkCredential
        {
            get
            {
                if (_networkCredential == null)
                    _networkCredential = new NetworkCredential(Username, Password);
                return _networkCredential;
            }
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
                IMedia newmedia = AddFile(_folder + newPath, item.Created == default(DateTime) ? item.Modified : item.Created, item.Modified == default(DateTime) ? item.Created : item.Modified);
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

        private bool _ftpDirectoryList()
        {
            bool exists = true;
            try
            {
                using (FtpClient _ftpClient = new FtpClient())
                {
                    Uri uri = new Uri(_folder, UriKind.Absolute);
                    _ftpClient.Host = uri.Host;
                    _ftpClient.Credentials = NetworkCredential;
                    _ftpClient.Connect();
                    ClearFiles();
                    foreach (var file in _ftpClient.GetListing(uri.LocalPath))
                        _ftpAddFileFromPath(_ftpClient, uri.LocalPath, "", file);
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
                    if (Monitor.TryEnter(_xdcamLockObject, 1000))
                        try
                        {
                            if (AccessType == TDirectoryAccessType.FTP)
                            {
                                using (XdcamClient client = new XdcamClient())
                                {
                                    Uri uri = new Uri(_folder, UriKind.Absolute);
                                    client.Host = uri.Host;
                                    client.Credentials = NetworkCredential;
                                    client.Connect();
                                    VolumeFreeSize = client.GetFreeDiscSpace();
                                    _readXDCAM(client);
                                    client.Disconnect();
                                }
                            }
                            else
                                _readXDCAM(null);
                        }
                        finally
                        {
                            Monitor.Exit(_xdcamLockObject);
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

        private XDCAM.Index _xDCAMIndex;

        internal void LockXDCAM(bool value)
        {
            if (IsXDCAM)
            {
                if (value)
                {
                    Debug.WriteLine("XDCAM about to lock");
                    Monitor.Enter(_xdcamLockObject);
                    Debug.WriteLine("XDCAM locked");
                }
                else
                {
                    Monitor.Exit(_xdcamLockObject);
                    Debug.WriteLine("XDCAM unlocked");
                }
            }
        }

        public override void SweepStaleMedia()
        {
            DateTime currentDateTime = DateTime.UtcNow.Date;
            List<IMedia> StaleMediaList = FindMediaList(m => currentDateTime > m.LastUpdated.Date + TimeSpan.FromDays(MediaRetnentionDays));
            foreach (IMedia m in StaleMediaList)
                m.Delete();
        }

        private XmlDocument _readXMLDocument(string documentName, FtpClient client)
        {
            XmlDocument xMLDoc = new XmlDocument();
            if (AccessType == TDirectoryAccessType.Direct)
                xMLDoc.Load(Path.Combine(_folder, documentName));
            if (AccessType == TDirectoryAccessType.FTP)
            {
                using (Stream stream = client.OpenRead(documentName))
                {
                    xMLDoc.Load(stream);
                }
            }
            Debug.WriteLineIf(xMLDoc == null, string.Format("_readXMLDocument didn\'t read {0}", documentName));
            return xMLDoc;
        }

        private void _readXDCAM(XdcamClient client)
        {
            try
            {
                _xDCAMIndex = XDCAM.SerializationHelper<XDCAM.Index>.Deserialize(_readXMLDocument("INDEX.XML", client));
                if (_xDCAMIndex != null)
                {
                    ClearFiles();
                    foreach (XDCAM.Index.Clip clip in _xDCAMIndex.clipTable.clipTable)
                        try
                        {
                            XDCAM.Index.Meta xmlClipFileNameMeta = clip.meta.FirstOrDefault(m => m.type == "PD-Meta");
                            if (xmlClipFileNameMeta != null && !string.IsNullOrWhiteSpace(xmlClipFileNameMeta.file))
                                clip.ClipMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXMLDocument(@"Clip/" + xmlClipFileNameMeta.file, client));
                            if (clip.ClipMeta != null)
                            {
                                IngestMedia newMedia = AddFile(string.Join(this.PathSeparator.ToString(), _folder, "Clip", clip.clipId + ".MXF"), clip.ClipMeta.CreationDate.Value, clip.ClipMeta.lastUpdate, new Guid(clip.ClipMeta.TargetMaterial.umidRef.Substring(32, 32))) as IngestMedia;
                                if (newMedia != null)
                                {
                                    newMedia.MediaName = clip.clipId;
                                    newMedia.Duration = ((long)clip.dur).SMPTEFramesToTimeSpan(clip.fps);
                                    newMedia.DurationPlay = newMedia.Duration;
                                    if (clip.aspectRatio == "4:3")
                                        newMedia.VideoFormat = TVideoFormat.PAL;
                                    if (clip.aspectRatio == "16:9")
                                        newMedia.VideoFormat = TVideoFormat.PAL_FHA;
                                    newMedia.ClipMetadata = clip.ClipMeta;
                                    if (clip.ClipMeta != null)
                                    {
                                        RationalNumber rate = new RationalNumber(clip.ClipMeta.LtcChangeTable.tcFps, 1);
                                        XDCAM.NonRealTimeMeta.LtcChange start = clip.ClipMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                        if (start != null)
                                        {
                                            TimeSpan tcStart = start.value.LTCTimecodeToTimeSpan(rate);
                                            if (tcStart >= TimeSpan.FromHours(40)) // TC 40:00:00:00 and greater
                                                tcStart -= TimeSpan.FromHours(40);
                                            newMedia.TcStart = tcStart;
                                            newMedia.TcPlay = tcStart;
                                        }
                                        newMedia.Verified = true;
                                        newMedia.MediaStatus = TMediaStatus.Available;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    if (_xDCAMIndex.editlistTable != null && _xDCAMIndex.editlistTable.editlistTable != null)
                        foreach (XDCAM.Index.EditList edl in _xDCAMIndex.editlistTable.editlistTable)
                        {
                            try
                            {
                                XDCAM.Index.Meta xmlClipFileNameMeta = edl.meta.FirstOrDefault(m => m.type == "PD-Meta");
                                if (xmlClipFileNameMeta != null && !string.IsNullOrWhiteSpace(xmlClipFileNameMeta.file))
                                {
                                    edl.EdlMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXMLDocument(@"Edit/" + xmlClipFileNameMeta.file, client));
                                    edl.smil = XDCAM.SerializationHelper<XDCAM.Smil>.Deserialize(_readXMLDocument(@"Edit/" + edl.file, client));
                                    DateTime ts = edl.EdlMeta.lastUpdate == default(DateTime) ? edl.EdlMeta.CreationDate.Value : edl.EdlMeta.lastUpdate;
                                    IngestMedia newMedia = AddFile(string.Join(this.PathSeparator.ToString(), _folder, "Sub", edl.file), ts, ts, new Guid(edl.EdlMeta.TargetMaterial.umidRef.Substring(32, 32))) as IngestMedia;
                                    if (newMedia != null)
                                    {
                                        newMedia.Duration = ((long)edl.dur).SMPTEFramesToTimeSpan(edl.fps);
                                        newMedia.DurationPlay = newMedia.Duration;
                                        if (edl.aspectRatio == "4:3")
                                            newMedia.VideoFormat = TVideoFormat.PAL;
                                        if (edl.aspectRatio == "16:9")
                                            newMedia.VideoFormat = TVideoFormat.PAL_FHA;
                                        newMedia.ClipMetadata = edl.EdlMeta;
                                        newMedia.SmilMetadata = edl.smil;
                                        if (edl.EdlMeta != null)
                                        {
                                            XDCAM.NonRealTimeMeta.LtcChange start = edl.EdlMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                            if (start != null)
                                            {
                                                TimeSpan tcStart = start.value.LTCTimecodeToTimeSpan(new RationalNumber(edl.EdlMeta.LtcChangeTable.tcFps, 1));
                                                if (tcStart >= TimeSpan.FromHours(40)) // TC 40:00:00:00 and greater
                                                    tcStart -= TimeSpan.FromHours(40);
                                                newMedia.TcStart = tcStart;
                                                newMedia.TcPlay = tcStart;
                                            }
                                            newMedia.Verified = true;
                                            newMedia.MediaStatus = TMediaStatus.Available;
                                        }
                                    }
                                }
                            }

                            catch (Exception e)
                            {
                                Debug.WriteLine(e);
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

        protected override IMedia AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            IMedia m = null;
            if (Extensions == null
             || Extensions.Count() == 0
             || Extensions.Any(ext => ext == Path.GetExtension(fullPath).ToLowerInvariant())
             || (IsXDCAM && Path.GetExtension(fullPath).ToLowerInvariant() == XDCAM.Smil.FileExtension)
            )
            {
                if (Path.GetExtension(fullPath).ToLowerInvariant() == ".xml")
                    _bMDXmlFiles.Add(fullPath);
                else
                {
                    m = base.AddFile(fullPath, created, lastWriteTime, guid);
                    if (Extensions != null && Extensions.Length >0)
                    {
                        string mediaExtension = Path.GetExtension(fullPath).ToLowerInvariant();
                        if (Extensions.Contains(mediaExtension))
                            m.MediaName = Path.GetFileNameWithoutExtension(fullPath);
                    }
                    if (IsXDCAM)
                        m.MediaName = Path.GetFileNameWithoutExtension(fullPath);
                }
            }
            return m;
        }

        protected override IMedia CreateMedia(string fullPath, Guid guid = default(Guid))
        {
            return new IngestMedia(this, guid)
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

        protected override void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.Name).ToLower() == ".xml" && _bMDXmlFiles.Contains(e.FullPath))
            {
                _scanXML(e.FullPath);
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
                if (IsXDCAM)
                    using (XdcamClient client = new XdcamClient())
                    {
                        Uri uri = new Uri(_folder, UriKind.Absolute);
                        client.Host = uri.Host;
                        client.Credentials = NetworkCredential;
                        client.Connect();
                        VolumeFreeSize = client.GetFreeDiscSpace();
                        client.Disconnect();
                    }
            }
            else
                base.GetVolumeInfo();
        }

    }

}
