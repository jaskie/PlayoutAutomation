using System.Collections.ObjectModel;
using System.Linq;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineStateViewmodel: ViewmodelBase
    {
        private readonly IEngine _engine;

        public EngineStateViewmodel(IEngine engine)
        {
            _engine = engine;
            _fixedTimeEvents = new ObservableCollection<EventPanelAutoStartEventViewmodel>(engine.FixedTimeEvents.Select(e => new EventPanelAutoStartEventViewmodel(e)));
            engine.FixedTimeEventOperation += _engine_FixedTimeEventOperation;
        }

        private void _engine_FixedTimeEventOperation(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Operation == CollectionOperation.Insert)
                _fixedTimeEvents.Add(new EventPanelAutoStartEventViewmodel(e.Item));
            if (e.Operation == CollectionOperation.Remove)
                _fixedTimeEvents.Remove(_fixedTimeEvents.FirstOrDefault(evm => evm.Event == e.Item));
        }


        readonly ObservableCollection<EventPanelAutoStartEventViewmodel> _fixedTimeEvents;
        public ObservableCollection<EventPanelAutoStartEventViewmodel> FixedTimeEvents { get { return _fixedTimeEvents; } }


        protected override void OnDispose()
        {
            _engine.FixedTimeEventOperation -= _engine_FixedTimeEventOperation;
        }
    }
}
