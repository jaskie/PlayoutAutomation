namespace TAS.Server.Common.Interfaces
{
    public interface IServerMedia: IPersistentMedia, IServerMediaProperties
    {
        bool IsArchived { get; }
    }

    public interface IServerMediaProperties: IPersistentMediaProperties
    {
        bool DoNotArchive { get; set; }
    }
}
