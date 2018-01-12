using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class CreateDatabaseViewmodel: OkCancelViewmodelBase<Model.CreateDatabase>
    {
        private string _connectionString;
        private string _collation;

        public CreateDatabaseViewmodel(): base(new Model.CreateDatabase(), typeof(CreateDatabaseView), "Create database") 
        {
            CommandEditConnectionString = new UICommand() { ExecuteDelegate = _editConnectionString };
        }

        public string ConnectionString { get { return _connectionString; } set { SetField(ref _connectionString, value); } }

        public string Collation { get { return _collation; } set { SetField(ref _collation, value); } }

        public static string[] Collations => Config.Model.CreateDatabase.Collations;

        public ICommand CommandEditConnectionString { get; }

        protected override void OnDispose() { }

        protected override void Ok(object o)
        {
            Update();
            if (Model.CreateEmptyDatabase())
                base.Ok(o);
        }

        private void _editConnectionString(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(ConnectionString))
            {
                vm.Load();
                if (vm.ShowDialog() == true)
                    ConnectionString = vm.Model.ConnectionString;
            }
        }
    }
}
