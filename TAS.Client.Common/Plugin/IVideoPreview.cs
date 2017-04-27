using System;

namespace TAS.Client.Common.Plugin
{
    public interface IVideoPreview: IDisposable
    {
        System.Windows.Controls.UserControl View { get; }
    }
}
