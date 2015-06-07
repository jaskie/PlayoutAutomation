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

namespace TAS.Server
{
    public class IngestDirectory : MediaDirectory
    {
        public string EncodeParams = string.Empty;
        
        private bool _deleteSource;
        public bool DeleteSource
        {
            get { return _deleteSource; }
            set { _deleteSource = value; }
        }

        public override void Initialize()
        {
            if (_folder.StartsWith("ftp://"))
                AccessType = TDirectoryAccessType.FTP;
            else
                if (!IsXDCAM) //not ftp and not xdcam
                {
                    if (string.IsNullOrWhiteSpace(Username)
                        || _connectToRemoteDirectory())
                        base.Initialize();
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

        public bool IsXDCAM { get; set; }

        public bool MediaDoNotArchive { get; set; }

        public int MediaRetnentionDays { get; set; }

        public TMediaCategory? MediaCategory { get; set; }

        private bool _ftpDirectoryList()
        {
            bool exists = true;
            try
            {
                using (client = new FtpClient())
                {
                    Uri uri = new Uri(_folder, UriKind.Absolute);
                    client.Host = uri.Host;
                    client.Credentials = NetworkCredential;
                    client.Connect();
                    _files.Clear();
                    foreach (string file in client.GetNameListing(uri.LocalPath))
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

        public override void Refresh()
        {
            if (IsXDCAM)
            {
                if (Monitor.TryEnter(_xdcamLockObject, 1000))
                    try
                    {
                        if (AccessType == TDirectoryAccessType.FTP)
                        {
                            using (client = new XdcamClient())
                            {
                                Uri uri = new Uri(_folder, UriKind.Absolute);
                                client.Host = uri.Host;
                                client.Credentials = NetworkCredential;
                                client.Connect();
                                _readXDCAM();
                            }
                        }
                        else
                            _readXDCAM();
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
                else
                    base.Refresh();
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
        
        private XmlDocument _readXMLDocument(string documentName)
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

        FtpClient client;

        private void _readXDCAM()
        {
            try
            {
                _xDCAMIndex = XDCAM.SerializationHelper<XDCAM.Index>.Deserialize(_readXMLDocument("/INDEX.XML"));
                if (_xDCAMIndex != null)
                {
                    _files.Clear();
                    foreach (XDCAM.Index.Clip clip in _xDCAMIndex.clipTable.clipTable)
                        try
                        {
                            IngestMedia newMedia = AddFile(clip.clipId + ".MXF") as IngestMedia;
                            if (newMedia != null)
                            {
                                newMedia._folder = "Clip";
                                newMedia._duration = TimeSpan.FromTicks(SMPTETimecode.FramesToTicks(clip.dur));
                                newMedia._durationPlay = newMedia.Duration;
                                if (clip.aspectRatio == "4:3")
                                    newMedia._videoFormat = TVideoFormat.PAL_43;
                                if (clip.aspectRatio == "16:9")
                                    newMedia._videoFormat = TVideoFormat.PAL_FHA;
                                XDCAM.Index.Meta xmlClipFileNameMeta = clip.meta.FirstOrDefault(m => m.type == "PD-Meta");
                                if (xmlClipFileNameMeta != null && !string.IsNullOrWhiteSpace(xmlClipFileNameMeta.file))
                                {
                                    clip.ClipMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXMLDocument(@"Clip/" + xmlClipFileNameMeta.file));
                                    newMedia.ClipMetadata = clip.ClipMeta;
                                    if (clip.ClipMeta != null)
                                    {
                                        newMedia._lastUpdated = clip.ClipMeta.lastUpdate == default(DateTime) ? clip.ClipMeta.CreationDate.Value : clip.ClipMeta.lastUpdate;
                                        newMedia.MediaGuid = new Guid(clip.ClipMeta.TargetMaterial.umidRef.Substring(32, 32));
                                        SMPTEFrameRate rate;
                                        switch (clip.ClipMeta.LtcChangeTable.tcFps)
                                        {
                                            case 30:
                                                rate = SMPTEFrameRate.SMPTERate30fps;
                                                break;
                                            case 24:
                                                rate = SMPTEFrameRate.SMPTERate24fps;
                                                break;
                                            default:
                                                rate = SMPTEFrameRate.SMPTERate25fps;
                                                break;
                                        }
                                        XDCAM.NonRealTimeMeta.LtcChange start = clip.ClipMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                        if (start != null)
                                        {
                                            TimeSpan tcStart = SMPTETimecode.LTCToTimeSpan(start.value, rate);
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

                                IngestMedia newMedia = AddFile(edl.file) as IngestMedia;
                                if (newMedia != null)
                                {
                                    newMedia._folder = "Clip";
                                    newMedia._duration = TimeSpan.FromTicks(SMPTETimecode.FramesToTicks(edl.dur));
                                    newMedia._durationPlay = newMedia.Duration;
                                    if (edl.aspectRatio == "4:3")
                                        newMedia._videoFormat = TVideoFormat.PAL_43;
                                    if (edl.aspectRatio == "16:9")
                                        newMedia._videoFormat = TVideoFormat.PAL_FHA;
                                    XDCAM.Index.Meta xmlClipFileNameMeta = edl.meta.FirstOrDefault(m => m.type == "PD-Meta");
                                    if (xmlClipFileNameMeta != null && !string.IsNullOrWhiteSpace(xmlClipFileNameMeta.file))
                                    {
                                        edl.EdlMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXMLDocument(@"Edit/" + xmlClipFileNameMeta.file));
                                        edl.smil = XDCAM.SerializationHelper<XDCAM.Smil>.Deserialize(_readXMLDocument(@"Edit/" + edl.file));
                                        newMedia.ClipMetadata = edl.EdlMeta;
                                        newMedia.SmilMetadata = edl.smil;
                                        if (edl.EdlMeta != null)
                                        {
                                            newMedia._lastUpdated = edl.EdlMeta.lastUpdate == default(DateTime) ? edl.EdlMeta.CreationDate.Value : edl.EdlMeta.lastUpdate;
                                            newMedia.MediaGuid = new Guid(edl.EdlMeta.TargetMaterial.umidRef.Substring(32, 32));
                                            SMPTEFrameRate rate;
                                            switch (edl.EdlMeta.LtcChangeTable.tcFps)
                                            {
                                                case 30:
                                                    rate = SMPTEFrameRate.SMPTERate30fps;
                                                    break;
                                                case 24:
                                                    rate = SMPTEFrameRate.SMPTERate24fps;
                                                    break;
                                                default:
                                                    rate = SMPTEFrameRate.SMPTERate25fps;
                                                    break;
                                            }
                                            XDCAM.NonRealTimeMeta.LtcChange start = edl.EdlMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                            if (start != null)
                                            {
                                                TimeSpan tcStart = SMPTETimecode.LTCToTimeSpan(start.value, rate);
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
        
        protected override Media AddFile(string fullPath, DateTime created = default(DateTime), DateTime lastWriteTime = default(DateTime))
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
                    {
                        m = _addFileFromFTP(fullPath);
                        m.Directory = this;
                    }
                    else
                        m = base.AddFile(fullPath, created, lastWriteTime);
                }
            }
            return m;
        }

        private Media _addFileFromFTP(string fileNameOnly)
        {
            Media newMedia = CreateMedia();
            newMedia._fileName = fileNameOnly;
            if (IsXDCAM)
            {
                newMedia._folder = "Clip";
            }
            newMedia._mediaName = (_extensions == null || _extensions.Count == 0) ? fileNameOnly : Path.GetFileNameWithoutExtension(fileNameOnly);
            newMedia._lastUpdated = DateTime.UtcNow;
            newMedia._mediaGuid = Guid.NewGuid();
            return newMedia;
        }
        
        protected override Media CreateMedia()
        {
            return new IngestMedia()
            {
                _mediaStatus = TMediaStatus.Unknown,
                _mediaCategory = this.MediaCategory ?? TMediaCategory.Uncategorized,
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
                            File.Delete((media as IngestMedia).XmlFile);
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

        protected override void EnumerateFiles()
        {
            base.EnumerateFiles();
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
                            m.TCStart = SMPTETimecode.TimecodeToTimeSpan(clip.SelectSingleNode(@"file/timecode/string").InnerText);
                            m.TCPlay = m.TCStart;
                            m.Duration = TimeSpan.FromTicks(SMPTETimecode.FramesToTicks(Int64.Parse(clip.SelectSingleNode(@"duration").InnerText)));
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
                       
        public bool Contains(string ClipName)
        {
            _files.Lock.EnterReadLock();
            try
            {
                return _files.Any(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.FileName)).ToUpper() == ClipName.ToUpper());
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }

        public string FindFileName(string ClipName)
        {
            Media m;
            _files.Lock.ExitReadLock();
            try
            {
                m = _files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.FileName)).ToUpper() == ClipName.ToUpper());
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
            if (m != null)
                return m.FullPath;
            return string.Empty;
        }

        public IngestMedia FindMedia(string ClipName)
        {
            _files.Lock.EnterReadLock();
            try
            {
                return (IngestMedia)_files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.FileName)).ToUpper() == ClipName.ToUpper());
            }
            finally
            {
                _files.Lock.ExitReadLock();
            }
        }


        //internal void IngestGet(IngestMedia media, ServerMedia serverMediaPGM, bool toTop)
        //{
        //    FileManager.Queue(
        //        new ConvertOperation
        //        {
        //            SourceMedia = media,
        //            DestMedia = serverMediaPGM,
        //        }, toTop);

        //}
    }

}
