using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable
    {
        UserControl View { get; }
        IEnumerable<RouterPort> GetInputPorts();
        bool SwitchInput(RouterPort inPort);
    }
}
