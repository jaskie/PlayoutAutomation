namespace TAS.Server.Common.Interfaces
{
    public interface IIngestMedia : IMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
