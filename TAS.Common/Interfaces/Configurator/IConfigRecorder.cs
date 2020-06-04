namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigRecorder : IRecorderProperties
    {
        new int Id { get; set; }
        new string RecorderName { get;  set; }
        new int DefaultChannel { get; set; }
        object Owner { get; set; }
    }
}
