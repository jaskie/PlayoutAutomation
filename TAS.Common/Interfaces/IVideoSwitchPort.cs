namespace TAS.Common.Interfaces
{
    public interface IVideoSwitchPort
    {
        short Id { get; }
        string Name { get; set; }
        bool? IsSignalPresent { get; }
    }

}
