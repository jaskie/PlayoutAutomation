using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaViewmodel: ViewmodelBase
    {
        public readonly ExportMedia MediaExport;
        public readonly IMediaManager MediaManager;
        private readonly ObservableCollection<ExportMediaLogoViewmodel> _logos;
        public ExportMediaViewmodel(IMediaManager manager, ExportMedia mediaExport)
        {
            this.MediaExport = mediaExport;
            MediaManager = manager;
            _logos = new ObservableCollection<ExportMediaLogoViewmodel>(mediaExport.Logos.Select(l => new ExportMediaLogoViewmodel(this, l)));
            CommandAddLogo = new UICommand() { ExecuteDelegate = _addLogo };
        }

        internal void Remove(ExportMediaLogoViewmodel exportMediaLogoViewModel)
        {
            _logos.Remove(exportMediaLogoViewModel);
            MediaExport.RemoveLogo(exportMediaLogoViewModel.Logo);
        }

        public string MediaName { get { return this.MediaExport.Media.MediaName; } }
        public TimeSpan StartTC { get { return this.MediaExport.StartTC; } set { SetField(ref this.MediaExport.StartTC, value); } }
        public TimeSpan Duration { get { return this.MediaExport.Duration; } set { SetField(ref this.MediaExport.Duration, value); } }
        public decimal AudioVolume { get { return this.MediaExport.AudioVolume; } set { SetField(ref this.MediaExport.AudioVolume, value); } }
        public ObservableCollection<ExportMediaLogoViewmodel> Logos { get { return _logos; } }
        public UICommand CommandAddLogo { get; private set; }

        private MediaSearchViewmodel _searchViewmodel;

        private void _addLogo(object o)
        {
            var svm = _searchViewmodel;
            if (svm == null)
            {
                svm = new MediaSearchViewmodel(
                    null, // preview
                    MediaManager,
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
