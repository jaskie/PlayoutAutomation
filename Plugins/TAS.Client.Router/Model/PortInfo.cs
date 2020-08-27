using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public class PortInfo
    {
        public PortInfo(short id, string name)
        {
            Id = id;
            Name = name;
        }       
        [Hibernate]
        public short Id { get; set; }
        [Hibernate]
        public string Name { get; set; }
    }   
}
