using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{
    public class CgElementViewModel : ModifyableViewModelBase
    {
        private string _name = string.Empty;
        private string _command = string.Empty;        
        private readonly Model.CgElement _cgElement;

        public CgElementViewModel(Model.CgElement cgElement)
        {
            LoadCommands();
            _cgElement = cgElement;
            LoadData();
        }
        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }

        private void LoadData()
        {
            _name = _cgElement.Name;
            _command = _cgElement.Command;
            IsModified = false;
        }        

        private void LoadCommands()
        {
        }

        private void ClearClientImage(object obj)
        {
        }

        public void Update(object _)
        {
            _cgElement.Name = _name;
            _cgElement.Command = _command;
        }

        protected override void OnDispose()
        {
            //
        }
    }
}
