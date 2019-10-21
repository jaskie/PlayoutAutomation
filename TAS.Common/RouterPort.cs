using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class RouterPort : IRouterPort
    {
        public RouterPort()
        {

        }

        public RouterPort(int id)
        {
            _portID = id;          
        }

        public RouterPort(int id, string name)
        {
            _portID = id;
            _portName = name;
        }

        public RouterPort(int id, bool isSignalPresent)
        {
            _portID = id;            
            _portIsSignalPresent = isSignalPresent;
        }

        private int _portID;
        public int PortID { 
            get => _portID; 
            set 
            {
                if (value == _portID)
                    return;
                _portID = value;
                NotifyPropertyChanged();
            }
        }

        private string _portName;
        public string PortName {
            get => _portName;
            set
            {
                if (value == _portName)
                    return;
                _portName = value;
                NotifyPropertyChanged();
            }
        }

        private bool? _portIsSignalPresent;
        public bool? PortIsSignalPresent {
            get => _portIsSignalPresent;
            set
            {
                if (value == _portIsSignalPresent)
                    return;
                _portIsSignalPresent = value;
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
