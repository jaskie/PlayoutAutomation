using System;
using System.ComponentModel;
using TAS.Common.Interfaces;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginContext: INotifyPropertyChanged
    {
        IEngine Engine { get; }
        void OnUiThread(Action action);
    }
}
