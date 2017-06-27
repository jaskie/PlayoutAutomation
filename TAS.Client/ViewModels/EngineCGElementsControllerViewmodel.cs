using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineCGElementsControllerViewmodel : ViewmodelBase, ICGElementsState
    {
        private const byte None = 0;
        private readonly ICGElementsController _controller;
        
        public EngineCGElementsControllerViewmodel(ICGElementsController controller)
        {
            _controller = controller;
            Crawls = controller.Crawls.Select(element => new CGElementViewmodel(element)).ToList();
            Logos = controller.Logos.Select(element => new CGElementViewmodel(element)).ToList();
            Parentals = controller.Parentals.Select(element => new CGElementViewmodel(element)).ToList();
            Auxes = controller.Auxes.Select(element => new CGElementViewmodel(element)).ToList();
            VisibleAuxes = controller.VisibleAuxes;
            controller.PropertyChanged += controller_PropertyChanged;
        }

        public byte Logo { get { return _controller?.Logo ?? None; }  set { if (_controller != null) _controller.Logo = value; } }

        public byte Crawl { get { return _controller?.Crawl ?? None; } set { if (_controller != null) _controller.Crawl = value; } }

        public byte Parental { get { return _controller?.Parental ?? None; } set { if (_controller != null) _controller.Parental = value; } }

        public IEnumerable<CGElementViewmodel> Crawls { get; private set; }

        public IEnumerable<CGElementViewmodel> Parentals { get; private set; }

        public IEnumerable<CGElementViewmodel> Logos { get; private set; }

        public IEnumerable<CGElementViewmodel> Auxes { get; private set; }

        public byte[] VisibleAuxes { get; private set; }

        public bool IsWideScreen { get { return _controller?.IsWideScreen ?? false; } set { if (_controller != null) _controller.IsWideScreen = value; } }

        public bool IsCGEnabled { get { return _controller?.IsCGEnabled ?? false; } set { if (_controller != null) _controller.IsCGEnabled = value; } }

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
            if (e.PropertyName == nameof(ICGElementsController.Auxes))
                Auxes = _controller.Auxes.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.VisibleAuxes))
                VisibleAuxes = _controller.VisibleAuxes.ToArray();
            NotifyPropertyChanged(e.PropertyName);
        }

    }
}
