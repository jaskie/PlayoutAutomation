namespace TAS.Server.Interfaces
{
    public interface IRecorder :IRecorderProperties
    {
        bool Play();
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
    }
}