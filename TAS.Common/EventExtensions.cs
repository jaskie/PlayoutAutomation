using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public static class EventExtensions
    {

        private static readonly string MixerFillCommand =
            $@"\s*MIXER\s+(?<layer>{
                string.Join("|", Enum.GetNames(typeof(VideoLayer)))
            })\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({
                string.Join("|", Enum.GetNames(typeof(TEasing)))
            })))?";

        public static readonly Regex RegexMixerFill = new Regex(MixerFillCommand, RegexOptions.IgnoreCase);

        private static readonly string MixerClipCommand =
            $@"\s*MIXER\s+(?<layer>{
                string.Join("|", Enum.GetNames(typeof(VideoLayer)))
            })\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({
                string.Join("|", Enum.GetNames(typeof(TEasing)))
            })))?";

        public static readonly Regex RegexMixerClip = new Regex(MixerClipCommand, RegexOptions.IgnoreCase);

        private static readonly string MixerClearCommand =
            $@"\s*MIXER\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+CLEAR\s*";

        public static readonly Regex RegexMixerClear = new Regex(MixerClearCommand, RegexOptions.IgnoreCase);

        private static readonly string PlayCommand =
                $@"\s*PLAY\s+(?<layer>{
                        string.Join("|", Enum.GetNames(typeof(VideoLayer)))
                    })\s+(?<file>\w+|""[\w\s]*"")(?<transition_block>\s+(?<transition_type>({
                        string.Join("|", Enum.GetNames(typeof(TTransitionType)))
                    }))\s+(?<transition_duration>[0-9]+)(\s+(?<easing>({
                        string.Join("|", Enum.GetNames(typeof(TEasing)))
                    })))?)?";

        public static readonly Regex RegexPlay = new Regex(PlayCommand, RegexOptions.IgnoreCase);

        public static bool IsValidCommand(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText)
                && (RegexMixerFill.IsMatch(commandText)
                || RegexMixerClip.IsMatch(commandText)
                || RegexMixerClear.IsMatch(commandText));
        }

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
            var pe = aEvent;
            while (pe != null)
            {
                yield return pe;
                pe = pe.GetVisualParent();
            }
        }


        public static IEvent GetVisualParent(this IEvent aEvent)
        {
            var curr = aEvent;
            var prior = curr.Prior;
            while (prior != null)
            {
                curr = prior;
                prior = curr.Prior;
            }
            return curr.Parent;
        }

        public static bool IsContainedIn(this IEvent aEvent, IEvent parent)
        {
            var pe = aEvent;
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
            if (aEvent.IsModified)
                Task.Run(() => aEvent.Save());
        }

        public static long LengthInFrames(this IEvent aEvent)
        {
            return aEvent.Length.Ticks / aEvent.Engine.FrameTicks; 
        }

        public static long TransitionInFrames(this IEvent aEvent)
        {
            return aEvent.TransitionTime.Ticks / aEvent.Engine.FrameTicks; 
        }


        /// <summary>
        /// Gets subsequent event that will play after this
        /// </summary>
        /// <returns></returns>
        public static IEvent GetSuccessor(this IEvent aEvent)
        {
            while (true)
            {
                var eventType = aEvent.EventType;
                if (eventType != TEventType.Movie && eventType != TEventType.Live && eventType != TEventType.Rundown)
                    return null;
                var current = aEvent;
                var next = current.Next;
                while (next != null && next.Length.Equals(TimeSpan.Zero))
                {
                    current = next;
                    next = current.GetSuccessor();
                }
                if (next != null)
                    return next;
                aEvent = current.GetVisualParent();
                if (aEvent == null)
                    return null;
            }
        }

        public static double GetAudioVolume(this IEvent aEvent)
        {
            var volume = aEvent.AudioVolume;
            if (volume != null)
                return volume.Value;
            if (aEvent.EventType == TEventType.Movie)
            {
                var m = aEvent.Media;
                if (m != null)
                    return m.AudioVolume;
            }
            return 0;
        }

        public static IEnumerable<IEvent> AllSubEvents(this IEvent e)
        {
            IEnumerable<IEvent> sel = e.SubEvents;
            foreach (var selItem in sel)
            {
                yield return selItem;
                var nextItem = selItem;
                while ((nextItem = nextItem.Next) != null)
                    yield return nextItem;
            }
        }
    }
}
