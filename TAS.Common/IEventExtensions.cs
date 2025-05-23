﻿using System;
using System.Collections.Generic;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public static class IEventExtensions
    {

        public static readonly string MixerFillCommand =
            $@"\s*MIXER\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({string.Join("|", Enum.GetNames(typeof(TEasing)))})))?";

        public static readonly string MixerClipCommand =
            $@"\s*MIXER\s+(?<layer>{
                string.Join("|", Enum.GetNames(typeof(VideoLayer)))
            })\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({
                string.Join("|", Enum.GetNames(typeof(TEasing)))
            })))?";

        public static readonly string MixerClearCommand = $@"\s*MIXER\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+CLEAR\s*";

        public static readonly string PlayCommand = $@"\s*PLAY\s+(?<layer>{string.Join("|", Enum.GetNames(typeof(VideoLayer)))})\s+(?<file>(.*))";

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
                current = current.GetNext();
            }
            return null;
        }

        public static IEvent FindInside(this IEvent aEvent, Func<IEvent, bool> searchFunc)
        {
            var se = aEvent.GetSubEvents();
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

        /// <summary>
        /// Finds AudioVolume for the Event or associated media
        /// </summary>
        /// <param name="aEvent">Event to find the value for</param>
        /// <returns>audio volume in relative dB, or 0 if unknown</returns>
        public static double GetAudioVolume(this IEvent aEvent)
        {
            var volume = aEvent.AudioVolume;
            if (volume.HasValue)
                return volume.Value;
            return aEvent.EventType == TEventType.Movie ? aEvent.Media?.AudioVolume ?? 0 : 0;
        }

        public static IEnumerable<IEvent> AllSubEvents(this IEvent e)
        {
            var sel = e.GetSubEvents();
            foreach (var selItem in sel)
            {
                yield return selItem;
                var nextItem = selItem;
                while ((nextItem = nextItem.GetNext()) != null)
                    yield return nextItem;
            }
        }

        public static bool IsVisibleEvent(this IEvent aEvent)
        {
            switch (aEvent.EventType)
            {
                case TEventType.Movie:
                case TEventType.StillImage:
                case TEventType.Live:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsAnimationOrCommandScript(this IEvent aEvent)
        {
            return aEvent != null && (aEvent.EventType == TEventType.Animation || aEvent.EventType == TEventType.CommandScript);
        }

        public static bool IsMovieOrStill(this IEvent aEvent)
        {
            return aEvent != null && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.StillImage);
        }

        public static bool IsMovieOrLive(this IEvent aEvent)
        {
            return aEvent != null && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live);
        }

        public static bool IsMovieOrLiveOnProgramLayer(this IEvent aEvent)
        {
            return aEvent?.Layer == VideoLayer.Program && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live);
        }

        public static bool IsMovieOrLiveOrRundown(this IEvent aEvent)
        {
            return aEvent != null && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live || aEvent.EventType == TEventType.Rundown);
        }

    }
}
