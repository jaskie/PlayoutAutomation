using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.ViewModels;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client
{

    internal static class EventClipboard
    {

        internal enum TPasteLocation { Under, Before, After };

        internal enum ClipboardOperation { Cut, Copy };

        static readonly List<IEventProperties> _undo = new List<IEventProperties>();
        /// <summary>
        /// original Undo location
        /// </summary>
        static IEvent _undoDest;
        static readonly List<IEventProperties> _clipboard = new List<IEventProperties>();
        static ClipboardOperation Operation;
        public static event Action ClipboardChanged;

        public static bool IsEmpty { get { return _clipboard.Count == 0; } }

        static void _notifyClipboardChanged()
        {
            ClipboardChanged?.Invoke();
        }
        #region Undo
        public static void SaveUndo(IEnumerable<IEvent> items, IEvent undoDest)
        {
            _undo.Clear();
            _undoDest = undoDest;
            foreach (var e in items)
                _undo.Add(EventProxy.FromEvent(e));
        }

        public static bool CanUndo()
        {
            return _undo.Count > 0 && _undoDest?.IsDeleted == false;
        }

        public static void Undo()
        {
            IEvent dest = _undoDest;
            using (var enumerator = _undo.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    TPasteLocation location = enumerator.Current.StartType == TStartType.After ? TPasteLocation.After : TPasteLocation.Under;
                    dest = _paste(enumerator.Current, dest, location, ClipboardOperation.Copy);
                    while (enumerator.MoveNext())
                        dest = _paste(enumerator.Current, dest, TPasteLocation.After, ClipboardOperation.Copy);
                }
            }
        }

        #endregion //Undo

        public static void Copy(IEnumerable<EventPanelViewmodelBase> items)
        {
            _clipboard.Clear();
            foreach (var e in items)
                _clipboard.Add(EventProxy.FromEvent(e.Event));
            Operation = ClipboardOperation.Copy;
            _notifyClipboardChanged();
        }

        public static void Cut(IEnumerable<EventPanelViewmodelBase> items)
        {
            _clipboard.Clear();
            foreach (var e in items)
                _clipboard.Add(e.Event);
            Operation = ClipboardOperation.Cut;
            _notifyClipboardChanged();
        }

        public static IEvent Paste(EventPanelViewmodelBase destination, TPasteLocation location)
        {
            IEvent dest = destination.Event;
            if (CanPaste(destination, location))
            {
                var operation = Operation;
                using (var enumerator = _clipboard.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                        return null;
                    dest = _paste(enumerator.Current, dest, location, operation);
                    while (enumerator.MoveNext())
                        dest = _paste(enumerator.Current, dest, TPasteLocation.After, operation);
                }
            }
            if (Operation == ClipboardOperation.Cut)
                _clipboard.Clear();
            return dest;
        }

        static IEvent _paste(IEventProperties source, IEvent dest, TPasteLocation location, ClipboardOperation operation)
        {
            if (operation == ClipboardOperation.Cut)
            {
                var sourceEvent = source as IEvent;
                if (sourceEvent != null)
                {
                    if (sourceEvent.Engine == dest.Engine)
                    {
                        sourceEvent.Remove();
                        switch (location)
                        {
                            case TPasteLocation.After:
                                dest.InsertAfter(sourceEvent);
                                break;
                            case TPasteLocation.Before:
                                dest.InsertBefore(sourceEvent);
                                break;
                            case TPasteLocation.Under:
                                dest.InsertUnder(sourceEvent,false);
                                break;
                        }
                        return sourceEvent;
                    }
                    else
                    {
                        //TODO: paste from another engine 
                        throw new NotImplementedException("Event engines are different");
                    }
                }
                else
                    throw new InvalidOperationException($"Cannot paste from type: {source?.GetType().Name}");
            }
            else //(operation == ClipboardOperation.Copy)
            {
                EventProxy sourceProxy = source as EventProxy;
                if (sourceProxy != null)
                {
                    var mediaFiles = (dest.Engine.MediaManager.MediaDirectoryPRI ?? dest.Engine.MediaManager.MediaDirectorySEC)?.GetFiles();
                    var animationFiles = (dest.Engine.MediaManager.AnimationDirectoryPRI ?? dest.Engine.MediaManager.AnimationDirectorySEC)?.GetFiles();
                    switch (location)
                    {
                        case TPasteLocation.After:
                            return sourceProxy.InsertAfter(dest, mediaFiles, animationFiles);
                        case TPasteLocation.Before:
                            return sourceProxy.InsertBefore(dest, mediaFiles, animationFiles);
                        case TPasteLocation.Under:
                            var newEvent = sourceProxy.InsertUnder(dest, false, mediaFiles, animationFiles);
                            if (dest.EventType == TEventType.Container)
                                newEvent.ScheduledTime = DateTime.UtcNow;
                            return newEvent;
                    }
                    throw new InvalidOperationException("Invalid paste location");
                }
                else
                    throw new InvalidOperationException($"Cannot paste from type: {source?.GetType().Name}");
            }
            
        }


        public static bool CanPaste(EventPanelViewmodelBase destEventVm, TPasteLocation location)
        {
            if (destEventVm?.Event == null)
                return false;
            IEventProperties dest = destEventVm.Event;
            var operation = Operation;
            var destStartType = dest.StartType;
            if (location != TPasteLocation.Under 
                && (destStartType == TStartType.Manual || destStartType == TStartType.OnFixedTime) 
                && _clipboard.Any(e => e.EventType != TEventType.Rundown))
                return false;
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
            return true;
        }


        private static bool _canPaste(IEventProperties source, IEventProperties dest, TPasteLocation location, ClipboardOperation operation)
        {
            var sourceEvent = source as IEvent;
            var destEvent = dest as IEvent;
            if (operation == ClipboardOperation.Cut
                && (destEvent == null || sourceEvent?.Engine != destEvent.Engine))
                return false;
            if (location == TPasteLocation.Under)
            {
                if (destEvent.EventType == TEventType.StillImage)
                    return false;
                if ((destEvent.EventType == TEventType.Movie || destEvent.EventType == TEventType.Live) && !(source.EventType == TEventType.StillImage ))
                    return false;
                if (destEvent.EventType == TEventType.Rundown && (source.EventType == TEventType.StillImage || destEvent.SubEvents.Count > 0))
                    return false;
                if (destEvent.EventType == TEventType.Container && source.EventType != TEventType.Rundown)
                    return false;
            }
            if (location == TPasteLocation.After || location == TPasteLocation.Before)
            {
                if (!(source.EventType == TEventType.Rundown
                   || source.EventType == TEventType.Movie
                   || source.EventType == TEventType.Live)
                ||
                    !(dest.EventType == TEventType.Rundown
                   || dest.EventType == TEventType.Movie
                   || dest.EventType == TEventType.Live)
                   )
                    return false;
            }
            if (operation == ClipboardOperation.Cut && destEvent.IsContainedIn(sourceEvent))
                return false;
            return true;
        }
    }
}
