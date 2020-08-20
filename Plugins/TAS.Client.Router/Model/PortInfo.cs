namespace TAS.Server.VideoSwitch.Model
{
    public class PortInfo
    {
        public PortInfo(short id, string name)
        {
            Id = id;
            Name = name;
        }       
        public short Id { get; set; }       
        public string Name { get; set; }
    }   
}
