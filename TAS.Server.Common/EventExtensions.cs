using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common
{
    public static class EventExtensions
    {

        private static readonly string MIXER_FILL_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        public static readonly Regex RegexMixerFill = new Regex(MIXER_FILL_COMMAND, RegexOptions.IgnoreCase);
        private static readonly string MIXER_CLIP_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        public static readonly Regex RegexMixerClip = new Regex(MIXER_CLIP_COMMAND, RegexOptions.IgnoreCase);
        private static readonly string MIXER_CLEAR_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLEAR\s*", string.Join("|", Enum.GetNames(typeof(VideoLayer))));
        public static readonly Regex RegexClearMixer = new Regex(MIXER_CLEAR_COMMAND, RegexOptions.IgnoreCase);
        private static readonly string PLAY_COMMAND = $@"\s*PLAY\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+(?<file>\w+|""[\w\s]*"")(?<transition_block>\s+(?<transition_type>({string.Join("|", Enum.GetNames(typeof(TTransitionType)))}))\s+(?<transition_duration>[0-9]+)(\s+(?<easing>({string.Join("|", Enum.GetNames(typeof(TEasing)))})))?)?";
        public static readonly Regex RegexPlay = new Regex(PLAY_COMMAND, RegexOptions.IgnoreCase);

        public static bool IsValidCommand(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText)
                && (RegexMixerFill.IsMatch(commandText)
                || RegexMixerClip.IsMatch(commandText)
                || RegexClearMixer.IsMatch(commandText));
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
            IEvent pe = aEvent;
            while (pe != null)
            {
                yield return pe;
                pe = pe.GetVisualParent();
            }
        }


        public static IEvent GetVisualParent(this IEvent aEvent)
        {
            IEvent curr = aEvent;
            IEvent prior = curr.Prior;
            while (prior != null)
            {
                curr = prior;
                prior = curr.Prior;
            }
            return curr.Parent;
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
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Movie || eventType == TEventType.Live || eventType == TEventType.Rundown)
            {
                IEvent current = aEvent;
                IEvent next = current.Next;
                while (next != null && next.Length.Equals(TimeSpan.Zero))
                {
                    current = next;
                    next = current.GetSuccessor();
                }
                if (next == null)
                    next = current.GetVisualParent()?.GetSuccessor();
                return next;
            }
            return null;
        }

        public static decimal GetAudioVolume(this IEvent aEvent)
        {
            var volume = aEvent.AudioVolume;
            if (volume != null)
                return (decimal)volume;
            else
                if (aEvent.EventType == TEventType.Movie)
            {
                var m = aEvent.Media;
                if (m != null)
                    return m.AudioVolume;
            }
            return 0m;
        }
        
    }
}
