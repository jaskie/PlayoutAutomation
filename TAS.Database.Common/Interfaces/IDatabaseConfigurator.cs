using System;
using System.Configuration;
using System.Windows.Controls;
using TAS.Common;

namespace TAS.Database.Common.Interfaces
{
    public interface IDatabaseConfigurator
    {
        DatabaseType DatabaseType { get; }
        UserControl View { get; }
        void Open(Configuration configuration);
        void Save();
        event EventHandler Modified;
    }
}
