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
            if (controller != null)
            {
                _crawls = controller.Crawls.ToList();
                _logos = controller.Logos.ToList();
                _parentals = controller.Parentals.ToList();
                controller.PropertyChanged += Controller_PropertyChanged;
            }
        }

        private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Controller != null)
            {
                if (e.PropertyName == nameof(ICGElementsController.Crawls))
                    _crawls = Controller.Crawls.ToList();
                if (e.PropertyName == nameof(ICGElementsController.Parentals))
                    _parentals = Controller.Parentals.ToList();
                if (e.PropertyName == nameof(ICGElementsController.Logos))
                    _logos = Controller.Logos.ToList();
            }
            NotifyPropertyChanged(e.PropertyName);
        }

        public byte Logo { get { return Controller == null ? None : Controller.Logo; }  set { if (Controller != null) Controller.Logo = value; } }
        public byte Crawl { get { return Controller == null ? None : Controller.Crawl; } set { if (Controller != null) Controller.Crawl = value; } }
        public byte Parental { get { return Controller == null ? None : Controller.Parental; } set { if (Controller != null) Controller.Parental = value; } }
        private IEnumerable<ICGElement> _crawls;
        public IEnumerable<ICGElement> Crawls { get { return _crawls; } }
        private IEnumerable<ICGElement> _parentals;
        public IEnumerable<ICGElement> Parentals { get { return _parentals; } }
        private IEnumerable<ICGElement> _logos;
        public IEnumerable<ICGElement> Logos { get { return _logos; } }

        public bool IsWideScreen { get { return Controller == null ? false : Controller.IsWideScreen; } set { if (Controller != null) Controller.IsWideScreen = value; } }
        public bool IsCGEnabled { get { return Controller == null ? false : Controller.IsCGEnabled; } set { if (Controller != null) Controller.IsCGEnabled = value; } }
        public bool IsMaster { get { return Controller == null ? false : Controller.IsMaster; } }
        public bool IsConnected { get { return Controller == null ? false : Controller.IsConnected; } }
        public bool Exists { get { return Controller != null; } }

        protected override void OnDispose()
        {
            if (Controller != null)
                Controller.PropertyChanged -= Controller_PropertyChanged;
        }


    }
}
