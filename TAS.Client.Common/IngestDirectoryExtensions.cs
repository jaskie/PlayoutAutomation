using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Common
{
    public static class IngestDirectoryExtensions
    {
        public static bool ContainsImport(this IIngestDirectoryProperties dir)
        {
            return dir.IsImport || dir.SubDirectories.Any(d => d.IsImport);
        }
        public static bool ContainsExport(this IIngestDirectoryProperties dir)
        {
            return dir.IsExport || dir.SubDirectories.Any(d => d.IsExport);
        }

        public static IEnumerable<IIngestDirectoryProperties> AllSubDirectories(this IEnumerable<IIngestDirectoryProperties> dirs)
        {
            foreach (var dir in dirs)
            {
                yield return dir;
                foreach (var subDir in dir.SubDirectories.AllSubDirectories())
                {
                    yield return subDir;
                }
            }
        }
    }
}
