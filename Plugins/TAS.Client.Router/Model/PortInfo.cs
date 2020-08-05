namespace TAS.Server.VideoSwitch.Model
{
    public class PortInfo
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
