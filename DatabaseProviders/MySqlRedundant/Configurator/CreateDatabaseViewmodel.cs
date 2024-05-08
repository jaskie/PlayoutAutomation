using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class CreateDatabaseViewmodel: OkCancelViewmodelBase<Model.CreateDatabase>
    {
        private string _connectionString;
        private string _collation;

        public CreateDatabaseViewmodel(DatabaseMySqlRedundant db) : base(new Model.CreateDatabase(db), typeof(CreateDatabaseView), "Create database") 
        {
            CommandEditConnectionString = new UiCommand(CommandName(nameof(EditConnectionString)), EditConnectionString);
        }

        public string ConnectionString
        {
            get => _connectionString;
            set => SetField(ref _connectionString, value);
        }

        public string Collation
        {
            get => _collation;
            set => SetField(ref _collation, value);
        }

        public static string[] Collations => Configurator.Model.CreateDatabase.Collations;

        public ICommand CommandEditConnectionString { get; }

        protected override void OnDispose() { }

        protected override void Ok(object o)
        {
            Update();
            if (Model.CreateEmptyDatabase())
                base.Ok(o);
        }

        private void EditConnectionString(object _)
        {
            using (var vm = new ConnectionStringViewmodel(ConnectionString))
            {
                if (vm.ShowDialog() == true)
                    ConnectionString = vm.Model.ConnectionString;
            }
        }
    }
}
