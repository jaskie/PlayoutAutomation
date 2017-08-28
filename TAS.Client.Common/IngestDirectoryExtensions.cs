using System.Linq;
using TAS.Common.Interfaces;

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
    }
}
