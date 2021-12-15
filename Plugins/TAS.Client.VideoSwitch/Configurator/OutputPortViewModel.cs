using TAS.Client.Common;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class OutputPortViewModel : ViewModelBase
    {
        private short _id;
        private string _name;
        private bool _isSelected;

        public OutputPortViewModel(short id, string name, bool isSelected)
        {
            _id = id;
            _name = name;
            _isSelected = isSelected;
        }


        public short Id { get => _id; set => SetField(ref _id, value); }

        public string Name { get => _name; set => SetField(ref _name, value); }

        public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }

        protected override void OnDispose()
        {

        }
    }
}
