using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class ArchiveDirectory : WatcherDirectory, IArchiveDirectory
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IArchiveDirectory.idArchive))]
        private ulong _idArchive;

        [JsonProperty(nameof(IArchiveDirectory.SearchMediaCategory))]
        private TMediaCategory? _searchMediaCategory;

        [JsonProperty(nameof(IArchiveDirectory.SearchString))]
        private string _searchString;

        #pragma warning restore

        public ulong idArchive
        {
            get => _idArchive;
            set => Set(value);
        }

        public TMediaCategory? SearchMediaCategory
        {
            get => _searchMediaCategory;
            set => Set(value);
        }
        
        public string SearchString
        {
            get => _searchString;
            set => Set(value);
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            return Query<IMedia>(parameters: new object[] { mediaProperties });
        }

        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<ArchiveMedia>>();
        }

        public IArchiveMedia Find(IMediaProperties media)
        {
            var ret =  Query<ArchiveMedia>(parameters: new object[] { media });
            return ret;
        }

        public void Search()
        {
            Invoke();
        }
    }
}
