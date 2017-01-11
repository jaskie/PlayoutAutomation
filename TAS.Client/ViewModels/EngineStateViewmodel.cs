using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TAS.Client.Views;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineStateViewmodel: ViewmodelBase
    {
        private readonly IEngine _engine;
        private readonly EngineStateView _engineStateView;

        public EngineStateViewmodel(IEngine engine)
        {
            _engine = engine;
            _fixedTimeEvents = new ObservableCollection<EventPanelAutoStartEventViewmodel>(engine.FixedTimeEvents.Select(e => new EventPanelAutoStartEventViewmodel(e)));
            engine.FixedTimeEventOperation += _engine_FixedTimeEventOperation;

            _engineStateView = new EngineStateView() { DataContext = this };
        }

        private void _engine_FixedTimeEventOperation(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Operation == TCollectionOperation.Insert)
                _fixedTimeEvents.Add(new EventPanelAutoStartEventViewmodel(e.Item));
            if (e.Operation == TCollectionOperation.Remove)
                _fixedTimeEvents.Remove(_fixedTimeEvents.FirstOrDefault(evm => evm.Event == e.Item));
        }

        public EngineStateView View { get { return _engineStateView; } }

        readonly ObservableCollection<EventPanelAutoStartEventViewmodel> _fixedTimeEvents;
        public ObservableCollection<EventPanelAutoStartEventViewmodel> FixedTimeEvents { get { return _fixedTimeEvents; } }


        protected override void OnDispose()
        {
            _engine.FixedTimeEventOperation -= _engine_FixedTimeEventOperation;
        }
    }
}
