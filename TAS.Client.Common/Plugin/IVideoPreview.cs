using System;
using System.ComponentModel.Composition;

namespace TAS.Client.Common.Plugin
{
    public interface IVideoPreview: IDisposable
    {
        System.Windows.Controls.UserControl View { get; }
        void SetSource(string sourceUrl);
    }
}
