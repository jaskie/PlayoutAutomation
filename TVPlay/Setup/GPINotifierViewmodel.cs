using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Setup
{
    public class GPINotifierViewmodel : OkCancelViewmodelBase<Server.GPINotifier, GPINotifierView>
    {
        public GPINotifierViewmodel(Server.GPINotifier model) : base(model) { }

        protected override void Close(object parameter)
        {
            
        }
        protected override void OnDispose()
        {

        }

        string _name;
        public string Name { get { return _name; } set { SetField(ref _name, value, "Name"); } }
        int _graphicsStartDelay;
        public int GraphicsStartDelay { get { return _graphicsStartDelay; } set { SetField(ref _graphicsStartDelay, value, "GraphicsStartDelay"); } }

        readonly Array _gPITypes = Enum.GetValues(typeof(Server.GPIType));
        public Array GPITypes { get { return _gPITypes; } }

        Server.GPIType _gpiType;
        public Server.GPIType Type { get { return _gpiType; } set { SetField(ref _gpiType, value, "Type"); } }

        string _address;
        public string Address { get { return _address; } set { SetField(ref _address, value, "Address"); } }
    }
}
