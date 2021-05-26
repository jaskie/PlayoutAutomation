namespace TAS.Common.Interfaces
{
    public interface IVideoSwitchPort
    {
        short Id { get; }
        string Name { get; }
        bool? IsSignalPresent { get; }
    }

}
