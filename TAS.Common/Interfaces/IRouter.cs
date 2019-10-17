using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable
    {       
        Task<IEnumerable<RouterPort>> GetInputPorts(bool requestCurrentInput = false);
        bool SwitchInput(IRouterPortState events);
        bool SwitchInput(RouterPort inPort);
        
        event EventHandler<RouterEventArgs> OnInputPortChangeReceived;        
    }
}
