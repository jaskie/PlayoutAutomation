namespace TAS.Common.Interfaces
{
    public interface IArchiveMedia: IPersistentMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
