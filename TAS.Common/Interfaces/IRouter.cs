using System.Collections.Generic;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter
    {
        UserControl View { get; }
        IEnumerable<RouterPort> GetInputPorts();
        bool SwitchInput(RouterPort inPort);
    }
}
