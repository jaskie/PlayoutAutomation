using System.ComponentModel;
using System.Runtime.CompilerServices;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public class PortInfo : IVideoSwitchPort, INotifyPropertyChanged
    {
        private short _id;
        private string _name;
        private bool? _isSignalPresent;

        public PortInfo(short id, string name)
        {
            _id = id;
            _name = name;
        }
        public PortInfo()
        {
        }

        public short Id
        {
            get => _id;
            set
            {
                if (_id == value)
                    return;
                _id = value;
                NotifyOfPropertyChanged();
            }
        }

        public string Name
        {
            get => _name; 
            set
            {
                if (_name == value)
                    return;
                _name = value;
                NotifyOfPropertyChanged();
            }
        }

        public bool? IsSignalPresent
        {
            get => _isSignalPresent; 
            set
            {
                if (_isSignalPresent == value)
                    return;
                _isSignalPresent = value;
                NotifyOfPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyOfPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
