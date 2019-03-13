using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server.XDCAM
{
    public class XdcamMedia : IngestMedia, IXdcamMedia
    {
        internal XdcamMaterial XdcamMaterial;

        private int _clipNr;

        [JsonProperty]
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

        public override void Verify(bool updateFormatAndDurations)
        {
            try
            {
                if (!(Directory is IngestDirectory dir))
                    throw new InvalidOperationException("XDCAMMedia: _directory is not IngestDirectory");
                if (XdcamMaterial == null)
                    return;
                if (!Monitor.TryEnter(dir.XdcamLockObject, 1000))
                    return;
                try
                {
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

        private System.Xml.XmlDocument ReadXml(string documentName)
        {
            if (!(Directory is IngestDirectory dir))
                throw new ApplicationException("Clip directory is not IngestDirectory");
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
            var metaFileName = XdcamMaterial.RelevantInfo?.FirstOrDefault(i => i.type == RelevantInfoType.Xml)?.uri?.TrimStart('.', '/');
            if (string.IsNullOrWhiteSpace(metaFileName))
                return false;
            var xdcamMeta = SerializationHelper<NonRealTimeMeta>.Deserialize(ReadXml(metaFileName));
            if (xdcamMeta == null)
                return false;
            LastUpdated = xdcamMeta.lastUpdate == default(DateTime)
                ? xdcamMeta.CreationDate.Value
                : xdcamMeta.lastUpdate;
            if (xdcamMeta.Title != null)
            {
                MediaName = string.IsNullOrWhiteSpace(xdcamMeta.Title.international)
                        ? xdcamMeta.Title.usAscii
                        : xdcamMeta.Title.international;
            }
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
