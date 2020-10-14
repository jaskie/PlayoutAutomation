using System;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaViewModel : ViewModelBase
    {
        private readonly IEngine _engine;

        public ExportMediaViewModel(IEngine engine, MediaExportDescription mediaExport)
        {
            _engine = engine;
            MediaExport = mediaExport;
            Logos = new ObservableCollection<ExportMediaLogoViewModel>(mediaExport.Logos.Select(l => new ExportMediaLogoViewModel(this, l)));
            CommandAddLogo = new UiCommand(_addLogo);
        }

        public string MediaName => MediaExport.Media.MediaName;

        public TimeSpan StartTC { get => MediaExport.StartTC; set => SetField(ref MediaExport.StartTC, value); }

        public TimeSpan Duration { get => MediaExport.Duration; set => SetField(ref MediaExport.Duration, value); }

        public double AudioVolume { get => MediaExport.AudioVolume; set => SetField(ref MediaExport.AudioVolume, value); }

        public ObservableCollection<ExportMediaLogoViewModel> Logos { get; }

        public UiCommand CommandAddLogo { get; }

        public MediaExportDescription MediaExport { get; }

        public TVideoFormat VideoFormat => _engine.VideoFormat;
        
        internal void Remove(ExportMediaLogoViewModel exportMediaLogoViewModel)
        {
            Logos.Remove(exportMediaLogoViewModel);
            MediaExport.RemoveLogo(exportMediaLogoViewModel.Logo);
        }

        private void _addLogo(object o)
        {
            using (var vm = new MediaSearchViewModel(
                null, // preview
                _engine,
                new[] { TMediaType.Still },
                VideoLayer.CG1,
                true, // close ater add
                MediaExport.Media.FormatDescription())
            {
                BaseEvent = null
            })
                if (WindowManager.Current.ShowDialog(vm) == true)
                {
                    Logos.Add(new ExportMediaLogoViewModel(this, vm.SelectedMedia));
                    MediaExport.AddLogo(vm.SelectedMedia);
                }
        }

        protected override void OnDispose() { }
    }
}
