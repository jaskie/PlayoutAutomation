using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;
using TAS.Remoting.Server;

namespace TAS.Server.Router.Model
{
    public class RouterPort : DtoBase, IRouterPort
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
        [JsonProperty]
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
        [JsonProperty]
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
        [JsonProperty]
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
    }
}
