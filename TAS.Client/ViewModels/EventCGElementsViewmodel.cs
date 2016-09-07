using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Server.Common;

namespace TAS.Client.ViewModels
{
    public class EventCGElementsViewmodel : EditViewmodelBase<EventCGElements>, Server.Interfaces.ICGElementsState
    {

        private readonly bool _enable;

        bool _isEnabled;
        public bool IsEnabled { get { return _enable && _isEnabled; } set { SetField(ref _isEnabled, value, nameof(IsEnabled)); } }

        byte _crawl;
        public byte Crawl { get { return _crawl; } set { SetField(ref _crawl, value, nameof(Crawl)); } }

        byte _logo;
        public byte Logo { get { return _logo; } set { SetField(ref _logo, value, nameof(Logo)); } }

        byte _parental;
        public byte Parental { get { return _parental; } set { SetField(ref _parental, value, nameof(Parental)); } }

        public EventCGElementsViewmodel(EventCGElements cgElements, bool enable): base(cgElements, null)
        {
            _enable = enable;
            if (cgElements != null)
                cgElements.PropertyChanged += cgElements_PropertyChanged;
        }

        private void cgElements_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        protected override void OnDispose()
        {
            if (Model != null)
                Model.PropertyChanged -= cgElements_PropertyChanged;
        }
    }
}
