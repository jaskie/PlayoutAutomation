using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class EventPanelContainerViewmodel: EventPanelViewmodelBase
    {
        public EventPanelContainerViewmodel(IEvent ev, EventPanelViewmodelBase parent): base(ev, parent) {
            if (ev.EventType != TEventType.Container)
                throw new ApplicationException(string.Format("Invalid panel type:{0} for event type:{1}", this.GetType(), ev.EventType));
            _isVisible = !HiddenEventsStorage.Contains(ev);
        }

        public ICommand CommandHide { get; private set; }
        public ICommand CommandShow { get; private set; }
        public ICommand CommandPaste { get { return _engineViewmodel.CommandPasteSelected; } }
        public ICommand CommandAddSubRundown { get; private set; }

        protected override void CreateCommands()
        {
            CommandHide = new UICommand()
            {
                ExecuteDelegate = o => IsVisible = false,
                CanExecuteDelegate = o => _isVisible
            };
            CommandShow = new UICommand()
            {
                ExecuteDelegate = o => IsVisible = true,
                CanExecuteDelegate = o => !_isVisible
            };
            CommandAddSubRundown = new UICommand()
            {
                ExecuteDelegate = _addSubRundown
            };
        }

        bool _isVisible;
        public override bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (SetField(ref _isVisible, value))
                {
                    if (value)
                        HiddenEventsStorage.Remove(_event);
                    else
                        HiddenEventsStorage.Add(_event);
                    if (!value)
                        IsSelected = false;
                    _root.NotifyContainerVisibility();
                }
            }
        }

        void _addSubRundown(object o)
        {
            _engineViewmodel.AddSimpleEvent(_event, TEventType.Rundown, true);
        }

        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            NotifyPropertyChanged(nameof(ChildrenCount));
        }

        public int ChildrenCount { get { return _event.SubEventsCount; } }


    }
}
