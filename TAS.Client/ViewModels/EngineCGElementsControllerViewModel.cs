using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineCGElementsControllerViewModel : ViewModelBase, ICGElementsState
    {
        private const byte None = 0;
        private readonly ICGElementsController _controller;
        
        public EngineCGElementsControllerViewModel(ICGElementsController controller)
        {
            _controller = controller;
            Crawls = controller.Crawls?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            Logos = controller.Logos?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            Parentals = controller.Parentals?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            controller.PropertyChanged += controller_PropertyChanged;
        }

        public byte Logo { get => _controller?.Logo ?? None; set { if (_controller != null) _controller.Logo = value; } }

        public byte Crawl { get => _controller?.Crawl ?? None; set { if (_controller != null) _controller.Crawl = value; } }

        public byte Parental { get => _controller?.Parental ?? None; set { if (_controller != null) _controller.Parental = value; } }

        public bool IsWideScreen { get => _controller.IsWideScreen; set { if (_controller != null) _controller.IsWideScreen = value; } }

        public CGElementViewModel[] Crawls { get; private set; }

        public CGElementViewModel[] Parentals { get; private set; }

        public CGElementViewModel[] Logos { get; private set; }        

        public bool IsCGEnabled { get => _controller?.IsCGEnabled ?? false; set { if (_controller != null) { _controller.IsCGEnabled = value; } } }

        public bool IsMaster => _controller?.IsMaster ?? false;

        public bool IsConnected => _controller?.IsConnected ?? false;

        protected override void OnDispose()
        {
            _controller.PropertyChanged -= controller_PropertyChanged;
        }

        private void controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.Crawls))
                Crawls = _controller.Crawls?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            if (e.PropertyName == nameof(ICGElementsController.Parentals))
                Parentals = _controller.Parentals?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            if (e.PropertyName == nameof(ICGElementsController.Logos))
                Logos = _controller.Logos?.Select(element => new CGElementViewModel(element)).ToArray() ?? new CGElementViewModel[0];
            NotifyPropertyChanged(e.PropertyName);
        }

    }
}
