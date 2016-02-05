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
        public bool DoNotEncode { get { return Get<bool>(); } set { Set(value); } }
        public TAspectConversion AspectConversion { get { return Get<TAspectConversion>(); } set { Set(value); } }
        public decimal AudioVolume { get { return Get<decimal>(); } set { Set(value); } }
        public bool DeleteSource { get { return Get<bool>(); } set { Set(value); } }
        public string EncodeParams { get { return Get<string>(); } set { Set(value); } }
        public string Filter { get { return Get<string>(); } set { Set(value); } }
        public bool IsWAN { get { return Get<bool>(); } set { Set(value); } }
        public bool IsXDCAM { get { return Get<bool>(); } set { Set(value); } }
        public bool IsRecursive { get { return Get<bool>(); } set { Set(value); } }
        public TMediaCategory MediaCategory { get { return Get<TMediaCategory>(); } set { Set(value); } }
        public bool MediaDoNotArchive { get { return Get<bool>(); } set { Set(value); } }
        public int MediaRetnentionDays { get { return Get<int>(); } set { Set(value); } }
        public TFieldOrder SourceFieldOrder { get { return Get<TFieldOrder>(); } set { Set(value); } }
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get { return Get<TxDCAMAudioExportFormat>(); } set { Set(value); } }
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get { return Get<TxDCAMVideoExportFormat>(); } set { Set(value); } }

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


        public override IEnumerable<IMedia> GetFiles()
        {
            var list = Query<List<IngestMedia>>();
            list.ForEach(m => m.Directory = this);
            return list.Cast<IMedia>().ToList(); ;
        }

    }
}
