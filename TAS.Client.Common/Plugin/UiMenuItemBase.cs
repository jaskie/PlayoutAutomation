using System;
using System.Collections.Generic;
using TAS.Common;

namespace TAS.Client.Common.Plugin
{
    public class UiMenuItemBase : IUiMenuItem
    {
        private readonly IUiPlugin _owner;
        private readonly List<UiMenuItemBase> _items;

        public UiMenuItemBase(IUiPlugin owner)
        {
            _owner = owner;
        }

        public void NotifyExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            _items?.ForEach(i => i.NotifyExecuteChanged());
        }

        public event EventHandler CanExecuteChanged;

        public string Header { get; set; }

        public IEnumerable<IUiMenuItem> Items => _items;

        public bool CanExecute(object parameter)
        {
            var ec = _owner.ExecutionContext?.Invoke();
            return ec?.Event != null && ec.Event.EventType == TEventType.Rundown;
        }

        public void Execute(object parameter)
        {
            var ec = _owner.ExecutionContext?.Invoke();
            ec?.Engine?.Start(ec.Event);
        }


    }
}