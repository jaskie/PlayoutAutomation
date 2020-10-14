using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineStateViewModel: ViewModelBase
    {
        private readonly IEngine _engine;

        public EngineStateViewModel(IEngine engine)
        {
            _engine = engine;
            _fixedTimeEvents = new ObservableCollection<EventPanelAutoStartEventViewModel>(engine.FixedTimeEvents.Select(e => new EventPanelAutoStartEventViewModel(e)));
            engine.FixedTimeEventOperation += _engine_FixedTimeEventOperation;
        }

        private void _engine_FixedTimeEventOperation(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Operation == CollectionOperation.Add)
                _fixedTimeEvents.Add(new EventPanelAutoStartEventViewModel(e.Item));
            if (e.Operation == CollectionOperation.Remove)
                _fixedTimeEvents.Remove(_fixedTimeEvents.FirstOrDefault(evm => evm.Event == e.Item));
        }


        readonly ObservableCollection<EventPanelAutoStartEventViewModel> _fixedTimeEvents;
        public ObservableCollection<EventPanelAutoStartEventViewModel> FixedTimeEvents => _fixedTimeEvents;


        protected override void OnDispose()
        {
            _engine.FixedTimeEventOperation -= _engine_FixedTimeEventOperation;
        }
    }
}
