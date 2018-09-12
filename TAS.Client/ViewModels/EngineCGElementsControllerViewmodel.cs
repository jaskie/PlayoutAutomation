using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineCGElementsControllerViewmodel : ViewModelBase, ICGElementsState
    {
        private const byte None = 0;
        private readonly ICGElementsController _controller;
        
        public EngineCGElementsControllerViewmodel(ICGElementsController controller)
        {
            _controller = controller;
            Crawls = controller.Crawls?.Select(element => new CGElementViewmodel(element)).ToList();
            Logos = controller.Logos?.Select(element => new CGElementViewmodel(element)).ToList();
            Parentals = controller.Parentals?.Select(element => new CGElementViewmodel(element)).ToList();
            controller.PropertyChanged += controller_PropertyChanged;
        }

        public byte Logo { get => _controller?.Logo ?? None; set { if (_controller != null) _controller.Logo = value; } }

        public byte Crawl { get => _controller?.Crawl ?? None; set { if (_controller != null) _controller.Crawl = value; } }

        public byte Parental { get => _controller?.Parental ?? None; set { if (_controller != null) _controller.Parental = value; } }

        public IEnumerable<CGElementViewmodel> Crawls { get; private set; }

        public IEnumerable<CGElementViewmodel> Parentals { get; private set; }

        public IEnumerable<CGElementViewmodel> Logos { get; private set; }

        public bool IsWideScreen { get => _controller?.IsWideScreen ?? false; set { if (_controller != null) _controller.IsWideScreen = value; } }

        public bool IsCGEnabled { get => _controller?.IsCGEnabled ?? false; set { if (_controller != null) { _controller.IsCGEnabled = value; } } }

        public bool IsMaster => _controller?.IsMaster ?? false;

        public bool IsConnected => _controller?.IsConnected ?? false;

        public bool Exists => _controller != null;

        protected override void OnDispose()
        {
            _controller.PropertyChanged -= controller_PropertyChanged;
        }

        private void controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.Crawls))
                Crawls = _controller.Crawls.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.Parentals))
                Parentals = _controller.Parentals.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.Logos))
                Logos = _controller.Logos.Select(element => new CGElementViewmodel(element)).ToList();
            NotifyPropertyChanged(e.PropertyName);
        }

    }
}
