using System;
using System.Collections.Generic;

namespace TAS.Server.Model
{
    internal class PortInfo
    {
        public PortInfo(short id, string name)
        {
            Id = id;
            Name = name;
        }
        public short Id { get; }
        public string Name { get; }
    }   
}
