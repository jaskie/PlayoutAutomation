using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server;

namespace TAS.Client.ViewModels
{
    internal static class EventClipboard
    {

        internal enum TPasteLocation { Under, Before, After };

        internal enum ClipboardOperation { Cut, Copy };

        static readonly SynchronizedCollection<Event> _clipboard = new SynchronizedCollection<Event>();
        static ClipboardOperation Operation;
        public static event Action ClipboardChanged;

        public static bool IsEmpty { get { return _clipboard.Count == 0; } }

        static void _notifyClipboardChanged()
        {
            var h = ClipboardChanged;
            if (h != null)
                h();
        }

        public static void Copy(IEnumerable<EventPanelViewmodel> items)
        {
            lock (_clipboard.SyncRoot)
            {
                _clipboard.Clear();
                foreach (EventPanelViewmodel e in items)
                    _clipboard.Add(e.Event);
                Operation = ClipboardOperation.Copy;
                _notifyClipboardChanged();
            }
        }

        public static void Cut(IEnumerable<EventPanelViewmodel> items)
        {
            lock (_clipboard.SyncRoot)
            {
                _clipboard.Clear();
                foreach (EventPanelViewmodel e in items)
                    _clipboard.Add(e.Event);
                Operation = ClipboardOperation.Cut;
                _notifyClipboardChanged();
            }
        }

        public static void Paste(EventPanelViewmodel destination, TPasteLocation location)
        {
            Event dest = destination.Event;
            lock(_clipboard.SyncRoot)
            {
                if (CanPaste(destination, location))
                {
                    var operation = Operation;
                    using (var enumerator = _clipboard.GetEnumerator())
                    {
                        if (!enumerator.MoveNext())
                            return;
                        dest = _paste(enumerator.Current, dest, location, operation);
                        while (enumerator.MoveNext())
                            dest = _paste(enumerator.Current, dest, TPasteLocation.After, operation);
                    }
                }
                if (Operation == ClipboardOperation.Cut)
                    _clipboard.Clear();
            }
        }

        static Event _paste(Event source, Event dest, TPasteLocation location, ClipboardOperation operation)
        {
            if (operation == ClipboardOperation.Cut && source.Engine == dest.Engine)
            {
                source.Remove();
                switch (location)
                {
                    case TPasteLocation.After:
                        dest.InsertAfter(source);
                        break;
                    case TPasteLocation.Before:
                        dest.InsertBefore(source);
                        break;
                    case TPasteLocation.Under:
                        dest.InsertUnder(source);
                        break;
                }
                return source;
            }

            if (operation == ClipboardOperation.Copy && source.Engine == dest.Engine)
            {
                Event newEvent = source.Clone();
                switch (location)
                {
                    case TPasteLocation.After:
                        dest.InsertAfter(newEvent);
                        break;
                    case TPasteLocation.Before:
                        dest.InsertBefore(newEvent);
                        break;
                    case TPasteLocation.Under:
                        if (dest.EventType == TEventType.Container)
                            newEvent.ScheduledTime = DateTime.UtcNow;
                        dest.InsertUnder(newEvent);
                        break;
                }
                return newEvent;
            }
            throw new ArgumentException("Event engines are different");
        }


        public static bool CanPaste(EventPanelViewmodel destEventVm, TPasteLocation location)
        {
            if (destEventVm == null)
                return false;
            Event dest = destEventVm.Event;
            lock (_clipboard.SyncRoot)
            {
                var operation = Operation;
                using (var enumerator = _clipboard.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                        return false;
                    if (!_canPaste(enumerator.Current, dest, location, operation))
                        return false;
                    dest = enumerator.Current;
                    while (enumerator.MoveNext())
                    {
                        if (!_canPaste(enumerator.Current, dest, TPasteLocation.After, operation))
                            return false;
                        dest = enumerator.Current;
                    }
                }
            }
            return true;
        }


        private static bool _canPaste(Event sourceEvent, Event destEvent, TPasteLocation location, ClipboardOperation operation)
        {
            if (sourceEvent.Engine != destEvent.Engine)
                return false;
            if (location == TPasteLocation.Under)
            {
                if (destEvent.EventType == TEventType.StillImage)
                    return false;
                if ((destEvent.EventType == TEventType.Movie || destEvent.EventType == TEventType.Live) && !(sourceEvent.EventType == TEventType.StillImage || sourceEvent.EventType == TEventType.AnimationFlash))
                    return false;
                if (destEvent.EventType == TEventType.Rundown && (sourceEvent.EventType == TEventType.StillImage || sourceEvent.EventType == TEventType.AnimationFlash || destEvent.SubEvents.Count > 0))
                    return false;
                if (destEvent.EventType == TEventType.Container && sourceEvent.EventType != TEventType.Rundown)
                    return false;
            }
            if (location == TPasteLocation.After || location == TPasteLocation.Before)
            {
                if (!(sourceEvent.EventType == TEventType.Rundown
                   || sourceEvent.EventType == TEventType.Movie
                   || sourceEvent.EventType == TEventType.Live)
                ||
                    !(destEvent.EventType == TEventType.Rundown
                   || destEvent.EventType == TEventType.Movie
                   || destEvent.EventType == TEventType.Live)
                   )
                    return false;
            }
            if (destEvent.IsContainedIn(sourceEvent))
            {
                if (sourceEvent == destEvent && location != TPasteLocation.Under && operation == ClipboardOperation.Copy)
                    return true;
                return false;
            }
            return true;
        }
    }
}
