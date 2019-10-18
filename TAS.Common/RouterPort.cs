using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common
{
    public class RouterPort : INotifyPropertyChanged
    {
        public RouterPort()
        {

        }

        public RouterPort(int id)
        {
            _id = id;          
        }

        public RouterPort(int id, string name)
        {
            _id = id;
            _name = name;
        }

        public RouterPort(int id, bool isSignalPresent)
        {
            _id = id;            
            _isSignalPresent = isSignalPresent;
        }

        private int _id;
        public int ID { 
            get => _id; 
            set 
            {
                if (value == _id)
                    return;
                _id = value;
                NotifyPropertyChanged();
            }
        }

        private string _name;
        public string Name {
            get => _name;
            set
            {
                if (value == _name)
                    return;
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private bool? _isSignalPresent;
        public bool? IsSignalPresent {
            get => _isSignalPresent;
            set
            {
                if (value == _isSignalPresent)
                    return;
                _isSignalPresent = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
