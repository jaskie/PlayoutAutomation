using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TAS.Common;
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
                if (searchFunc(current))
                    return current;
                var se = current.FindInside(searchFunc);
                if (se != null)
                    return se;
                current = current.Next;
            }
            return null;
        }

        public static IEvent FindInside(this IEvent aEvent, Func<IEvent, bool> searchFunc)
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

        public static IEnumerable<IEvent> GetVisualRootTrack(this IEvent aEvent)
        {
            IEvent pe = aEvent;
            while (pe != null)
            {
                yield return pe;
                pe = pe.GetVisualParent();
            }
        }


        public static IEvent GetVisualParent(this IEvent aEvent)
        {
            IEvent ev = aEvent;
            IEvent pev = ev.Prior;
            while (pev != null)
            {
                ev = ev.Prior;
                pev = ev.Prior;
            }
            return ev.Parent;
        }

        public static bool IsContainedIn(this IEvent aEvent, IEvent parent)
        {
            IEvent pe = aEvent;
            while (true)
            {
                if (pe == null)
                    return false;
                if (pe == parent)
                    return true;
                pe = pe.GetVisualParent();
            }
        }
        public static void SaveDelayed(this IEvent aEvent)
        {
            ThreadPool.QueueUserWorkItem(o => aEvent.Save());
        }

        public static long LengthInFrames(this IEvent aEvent)
        {
            return aEvent.Length.Ticks / aEvent.Engine.FrameTicks; 
        }

        public static bool IsFinished(this IEvent aEvent)
        {
            return aEvent.Position >= LengthInFrames(aEvent);
        }



        /// <summary>
        /// Gets subsequent event that will play after this
        /// </summary>
        /// <returns></returns>
        public static IEvent GetSuccessor(this IEvent aEvent)
        {
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Movie || eventType == TEventType.Live || eventType == TEventType.Rundown)
            {
                IEvent nev = aEvent.Next;
                if (nev != null)
                {
                    IEvent n = nev.Next;
                    while (nev != null && n != null && nev.Length.Equals(TimeSpan.Zero))
                    {
                        nev = nev.Next;
                        n = nev.Next;
                    }
                }
                if (nev == null)
                {
                    nev = aEvent.GetVisualParent();
                    if (nev != null)
                        nev = nev.GetSuccessor();
                }
                return nev;
            }
            return null;
        }




    }
}
