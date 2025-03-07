using System.Collections.Generic;
using System.ComponentModel;
namespace TAS.Common.Interfaces
{
    public interface IRouter : INotifyPropertyChanged
    {
        IRouterPort[] InputPorts { get; }
        /// <summary>
        /// Selects the input port with the given id, with optional switch delay.
        /// </summary>
        /// <param name="inputId">Id of the input</param>
        /// <param name="instant">If switch delay should be applied - when starting from rundown to compensate output delay</param>
        void SelectInputPort(int inputId, bool instant);
        IRouterPort SelectedInputPort { get; }
        /// <summary>
        /// If true, the engine will switch the input port on preload, about 2 seconds before the live event start.
        /// This is useful when router output is connected to CasparCG input.
        /// </summary>
        bool SwitchOnPreload { get; }
        bool IsConnected { get; }
    }
}
