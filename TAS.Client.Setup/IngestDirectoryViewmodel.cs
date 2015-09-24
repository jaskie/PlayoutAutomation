using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    [XmlType("IngestDirectory")]
    public class IngestDirectoryViewmodel: ViewModels.ViewmodelBase, IIngestDirectory
    {
        // only required by serializer
        public IngestDirectoryViewmodel()
        {
            _view = new IngestDirectoryView() { DataContext = this };
        }
        
        readonly System.Windows.Controls.UserControl _view;
        public IngestDirectoryViewmodel(IIngestDirectory directory): this()
        {
            _copyProperties(directory);
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
            _modified = false;
        }
        
        #region Enumerations
        Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        [XmlIgnore]
        public Array AspectConversions { get { return _aspectConversions; } }
        Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory));
        [XmlIgnore]
        public Array MediaCategories { get { return _mediaCategories; } }
        Array _sourceFieldOrders = Enum.GetValues(typeof(TFieldOrder));
        [XmlIgnore]
        public Array SourceFieldOrders { get { return _sourceFieldOrders; } }
        Array _xDCAMAudioExportFormats = Enum.GetValues(typeof(TxDCAMAudioExportFormat));
        [XmlIgnore]
        public Array XDCAMAudioExportFormats { get { return _xDCAMAudioExportFormats; } }
        Array _xDCAMVideoExportFormats = Enum.GetValues(typeof(TxDCAMVideoExportFormat));
        [XmlIgnore]
        public Array XDCAMVideoExportFormats { get { return _xDCAMVideoExportFormats; } }
        #endregion // Enumerations


        #region IIngestDirectory
        string _directoryName;
        public string DirectoryName { get { return _directoryName; } set { SetField(ref _directoryName, value, "DirectoryName"); } }
        string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value, "Folder"); } }
        string _username;
        [DefaultValue(default(string))]
        public string Username { get { return _username; } set { SetField(ref _username, value, "Username"); } }
        string _password;
        [DefaultValue(default(string))]
        public string Password { get { return _password; } set { SetField(ref _password, value, "Password"); } }
        TAspectConversion _aspectConversion;
        [DefaultValue(default(TAspectConversion))]
        public TAspectConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value, "AspectConversion"); } }
        double _audioVolume;
        [DefaultValue(default(double))]
        public double AudioVolume { get { return _audioVolume; } set { SetField(ref _audioVolume, value, "AudioVolume"); } }
        bool _deleteSource;
        [DefaultValue(false)]
        public bool DeleteSource { get { return _deleteSource; } set { SetField(ref _deleteSource, value, "DeleteSource"); } }
        bool _isXDCAM;
        [DefaultValue(false)]
        public bool IsXDCAM { get { return _isXDCAM; } set { SetField(ref _isXDCAM, value, "IsXDCAM"); } }
        TMediaCategory _mediaCategory;
        [DefaultValue(default(TMediaCategory))]
        public TMediaCategory MediaCategory { get { return _mediaCategory; } set { SetField(ref _mediaCategory, value, "MediaCategory"); } }
        bool _mediaDoNotArchive;
        [DefaultValue(false)]
        public bool MediaDoNotArchive { get { return _mediaDoNotArchive; } set { SetField(ref _mediaDoNotArchive, value, "MediaDoNotArchive"); } }
        int _mediaRetentionDays;
        [DefaultValue(default(int))]
        public int MediaRetnentionDays { get { return _mediaRetentionDays; } set { SetField(ref _mediaRetentionDays, value, "MediaRetnentionDays"); } }
        TFieldOrder _sourceFieldOrder;
        [DefaultValue(default(TFieldOrder))]
        public TFieldOrder SourceFieldOrder { get { return _sourceFieldOrder; } set { SetField(ref _sourceFieldOrder, value, "SourceFieldOrder"); } }
        TxDCAMAudioExportFormat _xDCAMAudioExportFormat;
        [DefaultValue(default(TxDCAMAudioExportFormat))]
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get { return _xDCAMAudioExportFormat; } set { SetField(ref _xDCAMAudioExportFormat, value, "XDCAMAudioExportFormat"); } }
        TxDCAMVideoExportFormat _xDCAMVideoExportFormat;
        [DefaultValue(default(TxDCAMVideoExportFormat))]
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get { return _xDCAMVideoExportFormat; } set { SetField(ref _xDCAMVideoExportFormat, value, "XDCAMVideoExportFormat"); } }
        
        #endregion // IIngestDirectory
        
        [XmlIgnore]
        public System.Windows.Controls.UserControl View { get { return _view; } }

        protected override void OnDispose()
        {
            
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField<T>(ref field, value, propertyName))
            {
                _modified = true;
                return true;
            }
            return false;
        }

        bool _modified;
        [XmlIgnore]
        public bool Modified { get { return _modified; } }


    }
}
