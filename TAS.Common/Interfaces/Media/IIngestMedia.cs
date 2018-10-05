namespace TAS.Common.Interfaces.Media
{
    public interface IIngestMedia : IMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
