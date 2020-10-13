using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class CreateDatabaseViewModel: OkCancelViewModelBase
    {
        private string _connectionString;
        private string _collation;
        private Model.CreateDatabase _createDatabase;

        public CreateDatabaseViewModel(DatabaseMySqlRedundant db) 
        {
            _createDatabase = new Model.CreateDatabase(db);
            CommandEditConnectionString = new UiCommand(_editConnectionString);
        }

        private void Init()
        {
            ConnectionString = _createDatabase.ConnectionString;
            Collation = _createDatabase.Collation;
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

        private void _editConnectionString(object obj)
        {
            using (var vm = new ConnectionStringViewModel(ConnectionString))
            {
                if (WindowManager.Current.ShowDialog(vm, "Edit connection parameters") == true)
                    ConnectionString = vm.ConnectionString;
            }
        }

        public override bool Ok(object obj)
        {
            _createDatabase.ConnectionString = ConnectionString;
            _createDatabase.Collation = Collation;
            if (_createDatabase.CreateEmptyDatabase())
                return true;
            
            return false;
        }        
    }
}
