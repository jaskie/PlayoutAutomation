using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{
    public interface ISearchableDirectory: IMediaDirectory
    {
        List<IMedia> Search(TMediaCategory? category, string searchString);
    }
}