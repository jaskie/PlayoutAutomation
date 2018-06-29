using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Media;

namespace TAS.Server.XDCAM
{
    public class XdcamMedia : IngestMedia, IXdcamMedia
    {
        //internal Clip XdcamClip;
        //internal Alias.ClipAlias XdcamAlias;
        //internal EditList XdcamEdl;
        internal Material XdcamMaterial;

        private int _clipNr;

        public XdcamMedia(IngestDirectory directory, Guid guid = default(Guid)) : base(directory, guid)
        {
        }

        public int ClipNr { get => _clipNr; set => SetField(ref _clipNr, value); }

        public override Stream GetFileStream(bool forWrite)
        {
            if (!(Directory is IngestDirectory dir))
                throw new InvalidOperationException("XDCAMMedia: _directory must be IngestDirectory");
            if (dir.AccessType != TDirectoryAccessType.Direct)
                return new XdcamStream(this, forWrite);
            var fileName = Path.Combine(dir.Folder, XdcamMaterial.uri);
            return new FileStream(fileName, forWrite ? FileMode.Create : FileMode.Open);
        }

        public override void Verify()
        {
            try
            {
                if (!(Directory is IngestDirectory dir))
                    throw new InvalidOperationException("XDCAMMedia: _directory is not IngestDirectory");
                if (Monitor.TryEnter(dir.XdcamLockObject, 1000))
                    try
                    {
                        //if (XdcamClip != null)
                        //{
                        //    var xmlFileName = XdcamAlias == null ? XdcamClip.clipId : XdcamAlias.value;
                        //    if (!string.IsNullOrWhiteSpace(xmlFileName))
                        //        XdcamMeta = SerializationHelper<NonRealTimeMeta>.Deserialize(ReadXml($"Clip/{xmlFileName}M01.XML"));
                        //    IsVerified = RedaXdcamMeta(XdcamClip.clipId);
                        //    if (IsVerified)
                        //        MediaStatus = TMediaStatus.Available;
                        //}
                        //if (XdcamEdl != null)
                        //{
                        //    string edlFileName = XdcamAlias == null ? XdcamEdl.editlistId : XdcamAlias.value;
                        //    if (!string.IsNullOrWhiteSpace(edlFileName))
                        //    {
                        //        XdcamMeta = SerializationHelper<NonRealTimeMeta>.Deserialize(ReadXml($"Edit/{edlFileName}M01.XML"));
                        //        XdcamSmil = SerializationHelper<Smil>.Deserialize(ReadXml($"Edit/{edlFileName}E01.SMI"));
                        //    }
                        //    IsVerified = RedaXdcamMeta(XdcamEdl.editlistId);
                        //    if (IsVerified)
                        //        MediaStatus = TMediaStatus.Available;
                        //}
                        if (XdcamMaterial == null)
                            return;
                        IsVerified = RedaXdcamMeta();
                        if (IsVerified)
                            MediaStatus = TMediaStatus.Available;
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

        private System.Xml.XmlDocument ReadXml(string documentName)
        {
            var dir = Directory as IngestDirectory;
            Debug.Assert(dir != null, "XDCAMMedia: Directory is not IngestDirectory");
            var client = dir.GetFtpClient();
            try
            {
                if (!client.IsConnected)
                    client.Connect();
                return dir.ReadXmlDocument(documentName, client);
            }
            finally
            {
                client.Disconnect();
            }
        }

        private bool RedaXdcamMeta()
        {
            var metaFileName = XdcamMaterial.RelevantInfo?.FirstOrDefault(i => i.type == RelevantInfoType.Xml)?.uri;
            if (string.IsNullOrWhiteSpace(metaFileName))
                return false;
            var xdcamMeta = SerializationHelper<NonRealTimeMeta>.Deserialize(ReadXml(metaFileName));
            if (xdcamMeta == null)
                return false;
            LastUpdated = xdcamMeta.lastUpdate == default(DateTime)
                ? xdcamMeta.CreationDate.Value
                : xdcamMeta.lastUpdate;
            MediaName = xdcamMeta.Title == null
                ? XdcamMaterial.uri
                : string.IsNullOrWhiteSpace(xdcamMeta.Title.usAscii)
                    ? xdcamMeta.Title.international
                    : xdcamMeta.Title.usAscii;

            var rate = new RationalNumber(xdcamMeta.LtcChangeTable.tcFps, 1);
            var start = xdcamMeta.LtcChangeTable.LtcChangeTable.FirstOrDefault(l => l.frameCount == 0);
            if (start == null)
                return true;
            TimeSpan tcStart = start.value.LTCTimecodeToTimeSpan(rate);
            if (tcStart >= TimeSpan.FromHours(40)) // TC 40:00:00:00 and greater
                tcStart -= TimeSpan.FromHours(40);
            TcStart = tcStart;
            TcPlay = tcStart;
            return true;
        }

    }
}
