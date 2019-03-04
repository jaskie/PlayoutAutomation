namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface IServerDirectory: IWatcherDirectory
    {
        bool IsRecursive { get; }
    }

}
