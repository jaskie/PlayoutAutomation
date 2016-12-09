using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class XDCAMMedia : IngestMedia, IXdcamMedia
    {
        public XDCAMMedia(IngestDirectory directory, Guid guid = default(Guid)) : base(directory, guid)
        {
        }
        internal XDCAM.Index.Clip XdcamClip;
        internal XDCAM.Alias.ClipAlias XdcamAlias;
        internal XDCAM.Index.EditList XdcamEdl;
        private int _clipNr;
        public int ClipNr { get { return _clipNr; } set { SetField(ref _clipNr, value, nameof(ClipNr)); } }

        public override Stream GetFileStream(bool forWrite)
        {
            var dir = _directory as IngestDirectory;
            if (dir != null)
            {
                if (Monitor.TryEnter(dir.XdcamLockObject, 1000))
                    try
                    {
                        if (dir.AccessType == TAS.Common.TDirectoryAccessType.Direct)
                        {
                            var fileName = Path.Combine(dir.Folder, "Clip", $"{(XdcamAlias != null ? XdcamAlias.clipId : XdcamClip.clipId)}.MXF");
                            return new FileStream(fileName, forWrite ? FileMode.Create : FileMode.Open);
                        }
                        else
                            return new XDCAM.XdcamStream(this, forWrite);
                    }
                    finally
                    {
                        Monitor.Exit(dir.XdcamLockObject);
                    }
                else
                    return null;                
            }
            throw new InvalidOperationException("XDCAMMedia: _directory must be IngestDirectory");
        }

        private System.Xml.XmlDocument _readXmlDocument(string documentName)
        {
            var dir = _directory as IngestDirectory;
            if (dir == null)
                throw new InvalidOperationException("XDCAMMedia: _directory is not IngestDirectory");
            var client = dir.GetFtpClient();
            try
            {
                if (!client.IsConnected)
                    client.Connect();
                return dir.ReadXMLDocument(documentName, client);
            }
            finally
            {
                client.Disconnect();
            }
        }

        internal override void Verify()
        {
            try
            {
                var dir = _directory as IngestDirectory;
                if (dir == null)
                    throw new InvalidOperationException("XDCAMMedia: _directory is not IngestDirectory");
                if (Monitor.TryEnter(dir.XdcamLockObject, 1000))
                    try
                    {
                        var clip = XdcamClip;
                        if (clip != null)
                        {
                            XDCAM.Index.Meta xmlClipFileNameMeta = clip.meta.FirstOrDefault(m => m.type == "PD-Meta");
                            string clipFileName = XdcamAlias == null ? clip.clipId : XdcamAlias.value;
                            if (!string.IsNullOrWhiteSpace(clipFileName))
                                clip.ClipMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXmlDocument($"Clip/{clipFileName}M01.XML"));
                            if (clip.ClipMeta != null)
                            {
                                LastUpdated = clip.ClipMeta.lastUpdate == default(DateTime) ? clip.ClipMeta.CreationDate.Value : clip.ClipMeta.lastUpdate;
                                MediaName =  clip.ClipMeta.Title == null ? clip.clipId : string.IsNullOrWhiteSpace(clip.ClipMeta.Title.usAscii) ? clip.ClipMeta.Title.international: clip.ClipMeta.Title.usAscii;
                                RationalNumber rate = new RationalNumber(clip.ClipMeta.LtcChangeTable.tcFps, 1);
                                XDCAM.NonRealTimeMeta.LtcChange start = clip.ClipMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                if (start != null)
                                {
                                    TimeSpan tcStart = start.value.LTCTimecodeToTimeSpan(rate);
                                    if (tcStart >= TimeSpan.FromHours(40)) // TC 40:00:00:00 and greater
                                        tcStart -= TimeSpan.FromHours(40);
                                    TcStart = tcStart;
                                    TcPlay = tcStart;
                                }
                                IsVerified = true;
                                MediaStatus = TMediaStatus.Available;
                            }
                        }
                        var edl = XdcamEdl;
                        if (edl != null)
                        {
                            XDCAM.Index.Meta xmlClipFileNameMeta = edl.meta.FirstOrDefault(m => m.type == "PD-Meta");
                            string edlFileName = XdcamAlias == null ? edl.editlistId : XdcamAlias.value;
                            if (!string.IsNullOrWhiteSpace(edlFileName))
                            {
                                edl.EdlMeta = XDCAM.SerializationHelper<XDCAM.NonRealTimeMeta>.Deserialize(_readXmlDocument($"Edit/{edlFileName}M01.XML"));
                                edl.smil = XDCAM.SerializationHelper<XDCAM.Smil>.Deserialize(_readXmlDocument($"Edit/{edlFileName}.SMI"));
                            }
                            if (edl.EdlMeta != null)
                            {
                                LastUpdated = edl.EdlMeta.lastUpdate == default(DateTime) ? edl.EdlMeta.CreationDate.Value : edl.EdlMeta.lastUpdate;
                                MediaName = edl.EdlMeta.Title == null ? edl.editlistId : string.IsNullOrWhiteSpace(edl.EdlMeta.Title.usAscii) ? edl.EdlMeta.Title.international : edl.EdlMeta.Title.usAscii;
                                RationalNumber rate = new RationalNumber(edl.EdlMeta.LtcChangeTable.tcFps, 1);
                                XDCAM.NonRealTimeMeta.LtcChange start = edl.EdlMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
                                if (start != null)
                                {
                                    TimeSpan tcStart = start.value.LTCTimecodeToTimeSpan(rate);
                                    if (tcStart >= TimeSpan.FromHours(40)) // TC 40:00:00:00 and greater
                                        tcStart -= TimeSpan.FromHours(40);
                                    TcStart = tcStart;
                                    TcPlay = tcStart;
                                }
                                IsVerified = true;
                                MediaStatus = TMediaStatus.Available;
                            }

                        }
                    }
                    finally
                    {
                        Monitor.Exit(dir.XdcamLockObject);
                    }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        internal void ReadAliasFile()
        {
            throw new NotImplementedException();
        }
    }
}
