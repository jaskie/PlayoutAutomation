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

namespace TAS.Server
{
    public class IngestDirectory : MediaDirectory, IIngestDirectoryConfig
    {
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
                            base.Initialize();
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
        
        public string EncodeParams {get; set;}
        
        public bool IsXDCAM { get; set; }

        public bool IsWAN { get; set; }

        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }

        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }

        public bool MediaDoNotArchive { get; set; }

        public int MediaRetnentionDays { get; set; }

        public TMediaCategory MediaCategory { get; set; }

        public decimal AudioVolume { get; set; }

        public TFieldOrder SourceFieldOrder { get; set; }

        public TAspectConversion AspectConversion { get; set; }

        public string Filter
        {
            get { return _filter; }
            set
            {
                if (!value.Equals(_filter))
                {
                    if (IsWAN)
                    {
                        CancelBeginWatch();
                        ClearFiles();
                        BeginWatch(value, false);
                    }
                    else
                        _filter = value;
                }
            }
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
                    foreach (string file in _ftpClient.GetNameListing(uri.LocalPath))
                        AddFile(file);
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
            IEnumerable<Media> StaleMediaList;
            _files.Lock.EnterReadLock();
            try
            {
                StaleMediaList = _files.Where(m => currentDateTime > m.LastUpdated.Date + TimeSpan.FromDays(MediaRetnentionDays)).ToList();
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            foreach (Media m in StaleMediaList)
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
                                IngestMedia newMedia = AddFile(clip.clipId + ".MXF", clip.ClipMeta.CreationDate.Value, clip.ClipMeta.lastUpdate, new Guid(clip.ClipMeta.TargetMaterial.umidRef.Substring(32, 32))) as IngestMedia;
                                if (newMedia != null)
                                {
                                    newMedia.Folder = "Clip";
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
                                            newMedia.TCStart = tcStart;
                                            newMedia.TCPlay = tcStart;
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
                                    IngestMedia newMedia = AddFile(edl.file, ts, ts, new Guid(edl.EdlMeta.TargetMaterial.umidRef.Substring(32, 32))) as IngestMedia;
                                    if (newMedia != null)
                                    {
                                        newMedia.Folder = "Clip";
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
                                                newMedia.TCStart = tcStart;
                                                newMedia.TCPlay = tcStart;
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
        
        protected override Media AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime), Guid guid = default(Guid))
        {
            Media m = null;
            if (_extensions == null 
             || _extensions.Count() == 0
             || _extensions.Any(ext => ext == Path.GetExtension(fullPath).ToLowerInvariant())
             || (IsXDCAM && Path.GetExtension(fullPath).ToLowerInvariant() == XDCAM.Smil.FileExtension)
            )
            {
                if (Path.GetExtension(fullPath).ToLowerInvariant() == ".xml")
                    _bMDXmlFiles.Add(fullPath);
                else
                {
                    if (AccessType == TDirectoryAccessType.FTP)
                        m = _addFileFromFTP(fullPath);
                    else
                        m = base.AddFile(fullPath, created, lastWriteTime, guid);
                }
            }
            return m;
        }

        private Media _addFileFromFTP(string fileNameOnly)
        {
            Media newMedia = CreateMedia(fileNameOnly);
            if (IsXDCAM)
            {
                newMedia.Folder = "Clip";
            }
            newMedia.MediaName = (_extensions == null || _extensions.Length == 0) ? fileNameOnly : Path.GetFileNameWithoutExtension(fileNameOnly);
            newMedia.LastUpdated = DateTime.UtcNow;
            NotifyMediaAdded(newMedia);
            return newMedia;
        }
        
        protected override Media CreateMedia(string fileNameOnly)
        {
            return new IngestMedia(this)
            {
                FileName = fileNameOnly,
                MediaStatus = TMediaStatus.Unknown,
                MediaCategory = this.MediaCategory,
            };
        }

        protected override Media CreateMedia(string fileNameOnly, Guid guid)
        {
            return new IngestMedia(this, guid)
            {
                FileName = fileNameOnly,
                MediaStatus = TMediaStatus.Unknown,
                MediaCategory = this.MediaCategory,
            };
        }

        protected override void FileRemoved(string fullPath)
        {
            if (Path.GetExtension(fullPath).ToLowerInvariant() == ".xml")
            {
                _bMDXmlFiles.Remove(fullPath);
                _files.Lock.EnterReadLock();
                try
                {
                    foreach (Media fd in _files.Where(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == fullPath))
                        ((IngestMedia)fd).XmlFile = string.Empty;
                }
                finally
                {
                    _files.Lock.ExitReadLock();
                }
            }
            else
                base.FileRemoved(fullPath);
        }

        public override void MediaRemove(Media media)
        {
            base.MediaRemove(media);
            // remove xmlfile if it was last media file
            if (media is IngestMedia && (media as IngestMedia).XmlFile != string.Empty)
            {
                _files.Lock.EnterReadLock();
                try
                {
                    if (!_files.Any(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == (media as IngestMedia).XmlFile))
                        try
                        {
                            string fn = (media as IngestMedia).XmlFile;
                            if (!string.IsNullOrWhiteSpace(fn) && File.Exists(fn))
                                File.Delete(fn);
                        }
                        catch { };
                }
                finally
                {
                    _files.Lock.ExitReadLock();
                }
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
                    _files.Lock.EnterReadLock();
                    try
                    {
                        foreach (Media fd in _files.Where(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == xf))
                            ((IngestMedia)fd).XmlFile = e.FullPath;
                    }
                    finally
                    {
                        _files.Lock.ExitReadLock();
                    }
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

        protected override void EnumerateFiles(string filter, CancellationToken cancelationToken)
        {
            base.EnumerateFiles(filter, cancelationToken);
            foreach (string xml in _bMDXmlFiles)
                _scanXML(xml);
        }

        // parse files from BMD's MediaExpress
        private void _scanXML(string xmlFileName)
        {
            _files.Lock.EnterReadLock();
            try
            {
                foreach (Media fd in _files.Where(f => (f is IngestMedia) && (f as IngestMedia).XmlFile == xmlFileName))
                    ((IngestMedia)fd).XmlFile = string.Empty;
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
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
                        IngestMedia m;
                        _files.Lock.EnterReadLock();
                        try
                        {
                            m = (IngestMedia)_files.FirstOrDefault(f => f.FileName == fileName);
                        }
                        finally
                        {
                            _files.Lock.ExitReadLock();
                        }
                        if (m != null)
                        {
                            m.TCStart = clip.SelectSingleNode(@"file/timecode/string").InnerText.SMPTETimecodeToTimeSpan(new RationalNumber(int.Parse(clip.SelectSingleNode(@"rate/timebase").InnerText), 1));
                            m.TCPlay = m.TCStart;
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
            _files.Lock.EnterReadLock();
            IngestMedia result = null;
            try
            {
                result = (IngestMedia)_files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.FileName)).ToLower() == clipNameLowered);
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
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
            if (IsXDCAM && AccessType == TDirectoryAccessType.FTP)
                using (XdcamClient client = new XdcamClient())
                {
                    Uri uri = new Uri(_folder, UriKind.Absolute);
                    client.Host = uri.Host;
                    client.Credentials = NetworkCredential;
                    client.Connect();
                    VolumeFreeSize = client.GetFreeDiscSpace();
                    client.Disconnect();
                }
            else
                base.GetVolumeInfo();
        }

    }

}
