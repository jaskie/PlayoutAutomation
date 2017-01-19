using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineCGElementsControllerViewmodel : ViewmodelBase, ICGElementsState
    {
        public readonly ICGElementsController Controller;
        const byte None = 0;
        public EngineCGElementsControllerViewmodel(ICGElementsController controller)
        {
            Controller = controller;
            _crawls = controller.Crawls.Select(element => new CGElementViewmodel(element)).ToList();
            _logos = controller.Logos.Select(element => new CGElementViewmodel(element)).ToList();
            _parentals = controller.Parentals.Select(element => new CGElementViewmodel(element)).ToList();
            _auxes = controller.Auxes.Select(element => new CGElementViewmodel(element)).ToList();
            _visibleAuxes = controller.VisibleAuxes;
            controller.PropertyChanged += controller_PropertyChanged;
        }

        private void controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.Crawls))
                _crawls = Controller.Crawls.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.Parentals))
                _parentals = Controller.Parentals.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.Logos))
                _logos = Controller.Logos.Select(element => new CGElementViewmodel(element)).ToList();
            if (e.PropertyName == nameof(ICGElementsController.Auxes))
                _auxes = Controller.Auxes.Select(element => new CGElementViewmodel(element)).ToList();
            NotifyPropertyChanged(e.PropertyName);
        }

        public byte Logo { get { return Controller == null ? None : Controller.Logo; }  set { if (Controller != null) Controller.Logo = value; } }
        public byte Crawl { get { return Controller == null ? None : Controller.Crawl; } set { if (Controller != null) Controller.Crawl = value; } }
        public byte Parental { get { return Controller == null ? None : Controller.Parental; } set { if (Controller != null) Controller.Parental = value; } }
        private IEnumerable<CGElementViewmodel> _crawls;
        public IEnumerable<CGElementViewmodel> Crawls { get { return _crawls; } }
        private IEnumerable<CGElementViewmodel> _parentals;
        public IEnumerable<CGElementViewmodel> Parentals { get { return _parentals; } }
        private IEnumerable<CGElementViewmodel> _logos;
        public IEnumerable<CGElementViewmodel> Logos { get { return _logos; } }
        private IEnumerable<CGElementViewmodel> _auxes;
        public IEnumerable<CGElementViewmodel> Auxes { get { return _auxes; } }
        private byte[] _visibleAuxes;
        public byte[] VisibleAuxes { get { return _visibleAuxes; } }

        public bool IsWideScreen { get { return Controller == null ? false : Controller.IsWideScreen; } set { if (Controller != null) Controller.IsWideScreen = value; } }
        public bool IsCGEnabled { get { return Controller == null ? false : Controller.IsCGEnabled; } set { if (Controller != null) Controller.IsCGEnabled = value; } }
        public bool IsMaster { get { return Controller == null ? false : Controller.IsMaster; } }
        public bool IsConnected { get { return Controller == null ? false : Controller.IsConnected; } }
        public bool Exists { get { return Controller != null; } }

        protected override void OnDispose()
        {
            Controller.PropertyChanged -= controller_PropertyChanged;
        }


    }
}
