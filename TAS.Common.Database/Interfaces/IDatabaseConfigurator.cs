using System;
using System.Configuration;
using System.Windows.Controls;

namespace TAS.Common.Database.Interfaces
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
