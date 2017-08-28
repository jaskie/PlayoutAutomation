namespace TAS.Common.Interfaces
{
    public interface IIngestMedia : IMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
