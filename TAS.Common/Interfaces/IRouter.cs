using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable
    {       
        void SwitchInput(IRouterPortState events);
        void SwitchInput(RouterPort inPort);

        void RequestInputPorts();
        void RequestSignalPresenceStates();
        void RequestCurrentInputPort();
        
        event EventHandler<RouterEventArgs> OnInputPortChange; 
        event EventHandler<RouterEventArgs> OnInputSignalPresenceListReceived;
        event EventHandler<RouterEventArgs> OnInputPortListChange;
    }
}
