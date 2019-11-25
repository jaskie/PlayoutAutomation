using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public static class EventExtensions
    {

        public static readonly string MixerFillCommand =
            $@"\s*MIXER\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({string.Join("|", Enum.GetNames(typeof(TEasing)))})))?";

        public static readonly string MixerClipCommand =
            $@"\s*MIXER\s+(?<layer>{
                string.Join("|", Enum.GetNames(typeof(VideoLayer)))
            })\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({
                string.Join("|", Enum.GetNames(typeof(TEasing)))
            })))?";

        public static readonly string MixerClearCommand =
            $@"\s*MIXER\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+CLEAR\s*";

        public static readonly string PlayCommand =
                $@"\s*PLAY\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+(?<file>((\[HTML\]\s+)?\S+|""[\w\s]*""))(?<transition_block>\s+(?<transition_type>({
                        string.Join("|", Enum.GetNames(typeof(TTransitionType)))
                    }))\s+(?<transition_duration>[0-9]+)(\s+(?<easing>({
                        string.Join("|", Enum.GetNames(typeof(TEasing)))
                    })))?)?";

        public static readonly string CallCommand =
                $@"\s*CALL\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+(?<function>(.+))";

        public static readonly string CgCommand =
            $@"CG\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+(?<method>{string.Join("|", Enum.GetNames(typeof(TemplateMethod)))})";
        
        public static readonly string CgWithLayerCommand = $@"{CgCommand}\s+(?<cg_layer>\d+)";
        
        public static readonly string CgAddCommand = $@"{CgWithLayerCommand}\s+(?<file>\w+|""[\w\s]*"")(\s+(?<play_on_load>0|1))?(\s+(?<data>\+|[\S\s]*))?";

        public static readonly string CgInvokeCommand = $@"{CgWithLayerCommand}\s+(?<cg_method>\w+)";

        public static readonly string CgUpdateCommand = $@"{CgWithLayerCommand}\s+(?<data>\w+|[\w\s]*)";

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

        public static void SaveDelayed(this IEventPesistent aEvent)
        {
            if (aEvent.IsModified)
                Task.Run(() => aEvent.Save());
        }



        public static long TransitionInFrames(this IEvent aEvent)
        {
            return aEvent.TransitionTime.Ticks / aEvent.Engine.FrameTicks; 
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
