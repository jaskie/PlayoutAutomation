using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable
    {
        UserControl View { get; }

        Task<IEnumerable<RouterPort>> GetInputPorts();
        bool SwitchInput(IRouterPortState events);

        event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        event EventHandler<RouterEventArgs> OnInputPortChangeReceived;        
    }
}
