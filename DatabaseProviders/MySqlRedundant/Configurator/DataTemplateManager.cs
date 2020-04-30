using System.ComponentModel.Composition;
using TAS.Client.Common;

namespace TAS.Database.MySqlRedundant.Configurator
{    
    [Export(typeof(IUiTemplatesManager))]
    public class DataTemplateManager : IUiTemplatesManager
    {
        public void LoadDataTemplates()
        {
            UiServices.AddDataTemplate(typeof(ConfiguratorViewModel), typeof(ConfiguratorView));
            UiServices.AddDataTemplate(typeof(ConnectionStringViewmodel), typeof(ConnectionStringView));
            UiServices.AddDataTemplate(typeof(CreateDatabaseViewmodel), typeof(CreateDatabaseView));
        }
    }
}
