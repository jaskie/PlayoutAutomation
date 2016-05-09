using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class IngestDirectory : MediaDirectory, IIngestDirectory
    {
        public bool DoNotEncode { get { return Get<bool>(); } set { SetField(value); } }
        public TAspectConversion AspectConversion { get { return Get<TAspectConversion>(); } set { SetField(value); } }
        public decimal AudioVolume { get { return Get<decimal>(); } set { SetField(value); } }
        public bool DeleteSource { get { return Get<bool>(); } set { SetField(value); } }
        public string EncodeParams { get { return Get<string>(); } set { SetField(value); } }
        public string ExportParams { get { return Get<string>(); } set { SetField(value); } }
        public string Filter { get { return Get<string>(); } set { Set(value); } }
        public bool IsWAN { get { return Get<bool>(); } set { SetField(value); } }
        public bool IsXDCAM { get { return Get<bool>(); } set { SetField(value); } }
        public bool IsRecursive { get { return Get<bool>(); } set { SetField(value); } }
        public bool IsExport { get { return Get<bool>(); } set { SetField(value); } }
        public bool IsImport { get { return Get<bool>(); } set { SetField(value); } }
        public TMediaCategory MediaCategory { get { return Get<TMediaCategory>(); } set { SetField(value); } }
        public bool MediaDoNotArchive { get { return Get<bool>(); } set { SetField(value); } }
        public int MediaRetnentionDays { get { return Get<int>(); } set { SetField(value); } }
        public TFieldOrder SourceFieldOrder { get { return Get<TFieldOrder>(); } set { SetField(value); } }
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
        public TMediaExportContainerFormat ExportContainerFormat { get; set; }
        public TVideoFormat ExportVideoFormat { get; set; }

        public string[] Extensions { get; set; }
        public NetworkCredential NetworkCredential { get { return null; } }
        public string Password { get; set; }
        public string Username { get; set; }


        public override IMedia FindMediaByDto(Guid dtoGuid)
        {
            IngestMedia result = Query<IngestMedia>(parameters: new[] { dtoGuid });
            result.Directory = this;
            return result;
        }
    }
}
