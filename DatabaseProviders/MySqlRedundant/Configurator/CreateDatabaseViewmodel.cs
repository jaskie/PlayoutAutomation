using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class CreateDatabaseViewmodel: EditViewmodelBase<Model.CreateDatabase>, IOkCancelViewModel
    {
        private string _connectionString;
        private string _collation;

        public CreateDatabaseViewmodel(DatabaseMySqlRedundant db) : base(new Model.CreateDatabase(db)) 
        {
            CommandEditConnectionString = new UiCommand(_editConnectionString);
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
            using (var vm = new ConnectionStringViewmodel(ConnectionString))
            {
                if (UiServices.WindowManager.ShowDialog(vm, "Edit connection parameters") == true)
                    ConnectionString = vm.Model.ConnectionString;
            }
        }

        public bool Ok(object obj)
        {
            Update();
            if (Model.CreateEmptyDatabase())
                return true;
            
            return false;
        }

        public void Cancel(object obj)
        {            
        }

        public bool CanOk(object obj)
        {
            return IsModified;
        }

        public bool CanCancel(object obj)
        {
            return true;
        }
    }
}
