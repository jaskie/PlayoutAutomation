namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IServerDirectory: IWatcherDirectory
    {
        bool IsRecursive { get; }
        string DirectoryName { get; }
        TMovieContainerFormat MovieContainerFormat { get; }
        bool IsPrimary { get; }
    }

}
