namespace TAS.Common.Interfaces.Media
{
    public interface IArchiveMedia: IPersistentMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
