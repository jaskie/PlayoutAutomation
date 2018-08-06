using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public struct MediaDeleteResult
    {
        public static MediaDeleteResult NoDeny = new MediaDeleteResult { Result = MediaDeleteResultEnum.Success };
        public enum MediaDeleteResultEnum
        {
            Success,
            InSchedule,
            Protected,
            Unknown,
            InsufficentRights
        }
        [JsonProperty]
        public MediaDeleteResultEnum Result;
        [JsonProperty]
        public IEventProperties Event;
        [JsonProperty]
        public IMedia Media;
    }
}
