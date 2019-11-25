using System;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    internal static class EventExtensions
    {
        public static bool CanMoveUp(this IEvent aEvent)
        {
            var prior = aEvent?.Prior;
            return prior != null && prior.PlayState == TPlayState.Scheduled &&
                   aEvent.PlayState == TPlayState.Scheduled && !aEvent.IsLoop
                   && (prior.StartType == TStartType.After || !aEvent.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }

        public static bool CanMoveDown(this IEvent aEvent)
        {
            var next = aEvent?.Next;
            return next != null && next.PlayState == TPlayState.Scheduled && aEvent.PlayState == TPlayState.Scheduled &&
                   !next.IsLoop
                   && (aEvent.StartType == TStartType.After || !next.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }

        public static DateTime DefaultScheduledTime => DateTime.Today.AddDays(1).AddHours(DateTime.Now.Hour).ToUniversalTime();
    }
}
