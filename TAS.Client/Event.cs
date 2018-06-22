using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    internal static class Event
    {
        public static bool CanMoveUp(this IEvent aEvent)
        {
            IEvent prior = aEvent?.Prior;
            return prior != null && prior.PlayState == TPlayState.Scheduled &&
                   aEvent.PlayState == TPlayState.Scheduled && !aEvent.IsLoop
                   && (prior.StartType == TStartType.After || !aEvent.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }

        public static bool CanMoveDown(this IEvent aEvent)
        {
            IEvent next = aEvent?.Next;
            return next != null && next.PlayState == TPlayState.Scheduled && aEvent.PlayState == TPlayState.Scheduled &&
                   !next.IsLoop
                   && (aEvent.StartType == TStartType.After || !next.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }
    }
}
