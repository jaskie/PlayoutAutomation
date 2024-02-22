using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

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

        public MediaDeleteResultEnum Result;

        public IEventProperties Event;

        public IMedia Media;
    }
}
