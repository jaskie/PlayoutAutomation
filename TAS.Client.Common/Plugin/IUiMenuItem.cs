using System.Collections.Generic;
using System.Windows.Input;

namespace TAS.Client.Common.Plugin
{
    public interface IUiMenuItem : ICommand
    {
        string Header { get; }
        IEnumerable<IUiMenuItem> Items { get; }
        void NotifyExecuteChanged();
    }
}