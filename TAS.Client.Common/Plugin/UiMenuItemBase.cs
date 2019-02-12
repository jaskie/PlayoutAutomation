using System;
using System.Collections.Generic;
using TAS.Common;

namespace TAS.Client.Common.Plugin
{
    public abstract class UiMenuItemBase : IUiMenuItem
    {
        protected readonly IUiPlugin Owner;

        protected UiMenuItemBase(IUiPlugin owner)
        {
            Owner = owner;
        }

        public IEnumerable<IUiMenuItem> Items { get; protected set; }

        public void NotifyExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            if (Items == null)
                return;
            foreach (var uiMenuItem in Items)
                uiMenuItem.NotifyExecuteChanged();
        }

        public event EventHandler CanExecuteChanged;

        public string Header { get; set; }


        public abstract bool CanExecute(object parameter);

        public abstract void Execute(object parameter);


    }
}