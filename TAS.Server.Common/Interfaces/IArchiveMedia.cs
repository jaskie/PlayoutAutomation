namespace TAS.Server.Common.Interfaces
{
    public interface IArchiveMedia: IPersistentMedia
    {
        TIngestStatus IngestStatus { get; }
    }
}
