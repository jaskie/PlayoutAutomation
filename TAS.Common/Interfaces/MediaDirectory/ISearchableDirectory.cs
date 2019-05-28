namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface ISearchableDirectory: IMediaDirectory
    {
        IMediaSearchProvider Search(TMediaCategory? category, string searchString);
    }
}