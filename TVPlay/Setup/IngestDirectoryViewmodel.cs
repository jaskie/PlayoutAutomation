using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class IngestDirectoryViewmodel: ViewModels.ViewmodelBase
    {
        readonly IIngestDirectory _directory;
        public IngestDirectoryViewmodel(IIngestDirectory directory)
        {

        }
        #region Enumerations
        Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversions { get { return _aspectConversions; } }
        Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory));
        public Array MediaCategories { get { return _mediaCategories; } }
        Array _sourceFieldOrders = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrders { get { return _sourceFieldOrders; } }
        Array _xDCAMAudioExportFormats = Enum.GetValues(typeof(TxDCAMAudioExportFormat));
        public Array XDCAMAudioExportFormats { get { return _xDCAMAudioExportFormats; } }
        Array _xDCAMVideoExportFormats = Enum.GetValues(typeof(TxDCAMVideoExportFormat));
        public Array XDCAMVideoExportFormats { get { return XDCAMVideoExportFormats; } }
        #endregion // Enumerations


        #region IIngestDirectory
        public string DirectoryName { get; set; }
        public TAspectConversion AspectConversion { get; set; }
        public double AudioVolume { get; set; }
        public bool DeleteSource { get; set; }
        public bool IsXDCAM { get; set; }
        public TMediaCategory MediaCategory { get; set; }
        public bool MediaDoNotArchive { get; set; }
        public int MediaRetnentionDays { get; set; }
        public TFieldOrder SourceFieldOrder { get; set; }
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
        #endregion // IIngestDirectory


        protected override void OnDispose()
        {
            
        }
    }
}
