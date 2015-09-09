using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class IngestDirectoryViewmodel: ViewModels.ViewmodelBase, IIngestDirectory
    {
        readonly System.Windows.Controls.UserControl _view;
        public IngestDirectoryViewmodel(IIngestDirectory directory)
        {
            _copyProperties(directory);
            _view = new IngestDirectoryView() { DataContext = this };
        }

        void _copyProperties(IIngestDirectory source)
        {
            PropertyInfo[] copiedProperties = this.GetType().GetProperties();
            foreach (PropertyInfo copyPi in copiedProperties)
            {
                PropertyInfo sourcePi = source.GetType().GetProperty(copyPi.Name);
                if (sourcePi != null)
                    copyPi.SetValue(this, sourcePi.GetValue(source, null), null);
            }
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
        public Array XDCAMVideoExportFormats { get { return _xDCAMVideoExportFormats; } }
        #endregion // Enumerations


        #region IIngestDirectory
        public string DirectoryName { get; set; }
        public string Folder { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
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

        public System.Windows.Controls.UserControl View { get { return _view; } }

        protected override void OnDispose()
        {
            
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
