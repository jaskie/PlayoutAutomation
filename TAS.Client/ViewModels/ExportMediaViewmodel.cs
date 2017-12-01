using System;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaViewmodel : ViewmodelBase
    {
        private readonly IMediaManager _mediaManager;
        public ExportMediaViewmodel(IMediaManager mediaManager, MediaExportDescription mediaExport)
        {
            MediaExport = mediaExport;
            _mediaManager = mediaManager;
            Logos = new ObservableCollection<ExportMediaLogoViewmodel>(mediaExport.Logos.Select(l => new ExportMediaLogoViewmodel(this, l)));
            CommandAddLogo = new UICommand { ExecuteDelegate = _addLogo };
        }

        public string MediaName => MediaExport.Media.MediaName;

        public TimeSpan StartTC { get { return MediaExport.StartTC; } set { SetField(ref MediaExport.StartTC, value); } }

        public TimeSpan Duration { get { return MediaExport.Duration; } set { SetField(ref MediaExport.Duration, value); } }

        public double AudioVolume { get { return MediaExport.AudioVolume; } set { SetField(ref MediaExport.AudioVolume, value); } }

        public ObservableCollection<ExportMediaLogoViewmodel> Logos { get; }

        public UICommand CommandAddLogo { get; }

        public MediaExportDescription MediaExport { get; }

        public TVideoFormat VideoFormat => _mediaManager.VideoFormat;



        internal void Remove(ExportMediaLogoViewmodel exportMediaLogoViewModel)
        {
            Logos.Remove(exportMediaLogoViewModel);
            MediaExport.RemoveLogo(exportMediaLogoViewModel.Logo);
        }

        private void _addLogo(object o)
        {
            using (var vm = new MediaSearchViewmodel(
                null, // preview
                _mediaManager,
                TMediaType.Still,
                VideoLayer.CG1,
                true, // close ater add
                MediaExport.Media.FormatDescription()))
                if (UiServices.ShowDialog<Views.MediaSearchView>(vm) == true)
                {
                    Logos.Add(new ExportMediaLogoViewmodel(this, vm.SelectedMedia));
                    MediaExport.AddLogo(vm.SelectedMedia);
                }
        }

        protected override void OnDispose() { }
    }
}
