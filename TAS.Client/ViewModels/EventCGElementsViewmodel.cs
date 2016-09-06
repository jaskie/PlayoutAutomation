using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.ViewModels
{
    public class EventCGElementsViewmodel : ViewmodelBase, Server.Interfaces.ICGElementsState
    {
        private readonly Server.Common.EventCGElements _cgElements;

        public bool IsEnabled { get { return _cgElements.IsEnabled; } set { _cgElements.IsEnabled = value; } }
        
        public byte Crawl { get { return _cgElements.Crawl; } set { _cgElements.Crawl = value; } }

        public byte Logo { get { return _cgElements.Logo; } set { _cgElements.Logo = value; } }

        public byte Parental { get { return _cgElements.Parental; } set { _cgElements.Parental = value; } }

        public EventCGElementsViewmodel(Server.Common.EventCGElements cgElements)
        {
            _cgElements = cgElements;
            if (cgElements != null)
                cgElements.PropertyChanged += cgElements_PropertyChanged;
        }


        private void cgElements_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        protected override void OnDispose()
        {
            if (_cgElements != null)
                _cgElements.PropertyChanged += cgElements_PropertyChanged;
        }
    }
}
