using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class Gpi: IGpiConfig
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int GraphicsStartDelay { get; set; } 
    }
}
