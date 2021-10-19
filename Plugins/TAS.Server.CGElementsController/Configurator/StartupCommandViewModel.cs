using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{
    internal class StartupCommandViewModel : ModifyableViewModelBase
    {
        private string _command;

        public StartupCommandViewModel(string command)
        {
            _command = command;
        }

        public string Command
        {
            get => _command;
            set => SetField(ref _command, value);
        }

        protected override void OnDispose()
        { }
    }
}
