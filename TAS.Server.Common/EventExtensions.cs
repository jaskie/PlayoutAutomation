using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public static class EventExtensions
    {

        public static IEvent FindNext(this IEvent startEvent, Func<IEvent, bool> searchFunc)
        {
            var current = startEvent;
            while (current != null)
            {
                var se = current.FindInside(searchFunc);
                if (se != null)
                    return se;
                current = current.GetSuccessor();
                if (current != null && searchFunc(current))
                    return current;
            }
            return null;
        }

        private static IEvent FindInside(this IEvent aEvent, Func<IEvent, bool> searchFunc)
        {
            var se = aEvent.SubEvents;
            foreach(var ev in se)
            {
                if (searchFunc(ev))
                    return ev;
                var fe = ev.FindNext(searchFunc);
                if (fe != null)
                    return fe;
            }
            return null;
        }
    }
}
