using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public class PortInfo: IVideoSwitchPort
    {
        public PortInfo(short id, string name)
        {
            Id = id;
            Name = name;
        }     
        public PortInfo() { }
        
        [Hibernate]
        public short Id { get; set; }
        
        [Hibernate]
        public string Name { get; set; }

        public bool? IsSignalPresent => throw new System.NotImplementedException();
    }   
}
