using System;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaViewmodel: ViewmodelBase
    {
        
        private readonly ObservableCollection<ExportMediaLogoViewmodel> _logos;
        private readonly IMediaManager _mediaManager;
        public ExportMediaViewmodel(IMediaManager mediaManager, MediaExportDescription mediaExport)
        {
            MediaExport = mediaExport;
            _mediaManager = mediaManager;
            _logos = new ObservableCollection<ExportMediaLogoViewmodel>(mediaExport.Logos.Select(l => new ExportMediaLogoViewmodel(this, l)));
            CommandAddLogo = new UICommand { ExecuteDelegate = _addLogo };
        }
        
        public string MediaName => MediaExport.Media.MediaName;
        public TimeSpan StartTC { get { return MediaExport.StartTC; } set { SetField(ref MediaExport.StartTC, value); } }
        public TimeSpan Duration { get { return MediaExport.Duration; } set { SetField(ref MediaExport.Duration, value); } }
        public decimal AudioVolume { get { return MediaExport.AudioVolume; } set { SetField(ref MediaExport.AudioVolume, value); } }
        public ObservableCollection<ExportMediaLogoViewmodel> Logos => _logos;
        public UICommand CommandAddLogo { get; }
        public MediaExportDescription MediaExport { get; }


        internal void Remove(ExportMediaLogoViewmodel exportMediaLogoViewModel)
        {
            _logos.Remove(exportMediaLogoViewModel);
            MediaExport.RemoveLogo(exportMediaLogoViewModel.Logo);
        }

        private MediaSearchViewmodel _searchViewmodel;

        private void _addLogo(object o)
        {
            var svm = _searchViewmodel;
            if (svm == null)
            {
                svm = new MediaSearchViewmodel(
                    null, // preview
                    _mediaManager,
                    TMediaType.Still, 
                    VideoLayer.CG1,
                    true, // close ater add
                    MediaExport.Media.FormatDescription());
                svm.MediaChoosen += _searchMediaChoosen;
                svm.SearchWindowClosed += _searchWindowClosed;
                svm.ExecuteAction = (e) =>
                {
                    _logos.Add(new ExportMediaLogoViewmodel(this, e.Media));
                    MediaExport.AddLogo(e.Media);
                };
                _searchViewmodel = svm;
            }
        }

        private void _searchMediaChoosen(object sender, MediaSearchEventArgs e)
        {
            if (((MediaSearchViewmodel)sender).ExecuteAction != null)
                ((MediaSearchViewmodel)sender).ExecuteAction(e);
        }

        private void _searchWindowClosed(object sender, EventArgs e)
        {
            MediaSearchViewmodel mvs = (MediaSearchViewmodel)sender;
            mvs.MediaChoosen -= _searchMediaChoosen;
            mvs.SearchWindowClosed -= _searchWindowClosed;
            _searchViewmodel.Dispose();
            _searchViewmodel = null;
        }
        
        protected override void OnDispose() { }
    }
}
