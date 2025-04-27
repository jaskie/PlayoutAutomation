using System;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    public static class IEventExtensions
    {
        public static bool CanMoveUp(this IEvent aEvent)
        {
            var prior = aEvent?.GetPrior();
            return prior != null && prior.PlayState == TPlayState.Scheduled &&
                   aEvent.PlayState == TPlayState.Scheduled && !aEvent.IsLoop
                   && (prior.StartType == TStartType.After || !aEvent.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }

        public static bool CanMoveDown(this IEvent aEvent)
        {
            var next = aEvent?.GetNext();
            return next != null && next.PlayState == TPlayState.Scheduled && aEvent.PlayState == TPlayState.Scheduled &&
                   !next.IsLoop
                   && (aEvent.StartType == TStartType.After || !next.IsHold)
                   && aEvent.HaveRight(EventRight.Modify);
        }

        public static DateTime DefaultScheduledTime => DateTime.Today.AddDays(1).AddHours(DateTime.Now.Hour).ToUniversalTime();
    }
}
