using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.ViewModels;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    internal static class EventClipboard
    {

        internal enum PasteLocation { Under, Before, After }

        internal enum ClipboardOperation { Cut, Copy }

        private static readonly List<IEventProperties> Undos = new List<IEventProperties>();
        /// <summary>
        /// original Undo location
        /// </summary>
        private static IEvent _undoDest;
        private static IEngine _undoEngine;

        private static readonly List<IEventProperties> Clipboard = new List<IEventProperties>();
        private static ClipboardOperation _operation;
        public static event Action ClipboardChanged;

        public static bool IsEmpty => Clipboard.Count == 0;

        static void _notifyClipboardChanged()
        {
            ClipboardChanged?.Invoke();
        }
        #region Undo
        public static void SaveUndo(List<IEvent> items, IEvent undoDest)
        {
            if (items == null)
                return;
            Undos.Clear();
            _undoDest = undoDest;
            _undoEngine = items.FirstOrDefault()?.Engine;
            foreach (var e in items)
                Undos.Add(EventProxy.FromEvent(e));
        }

        public static bool CanUndo()
        {
            if (_undoDest?.IsDeleted == true)
            {
                _clearUndo();
                return false;
            }
            return Undos.Count > 0 && _undoEngine != null && _undoDest?.HaveRight(EventRight.Create) == true;
        }

        public static void Undo()
        {
            using (var enumerator = Undos.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var dest = _pasteUndo(enumerator.Current);
                    while (enumerator.MoveNext())
                        dest = _paste(enumerator.Current, dest, PasteLocation.After, ClipboardOperation.Copy);
                }
            }
            _clearUndo();
        }

        private static void _clearUndo()
        {
            Undos.Clear();
            _undoDest = null;
            _undoEngine = null;
        }

        private static IEvent _pasteUndo(IEventProperties source)
        {
            if (!(source is EventProxy sourceProxy) || _undoEngine == null)
                throw new InvalidOperationException($"Cannot undo: {source.EventName}");
            var mediaFiles =
                (_undoEngine.MediaManager.MediaDirectoryPRI ?? _undoEngine.MediaManager.MediaDirectorySEC)
                ?.GetAllFiles();
            var animationFiles =
                (_undoEngine.MediaManager.AnimationDirectoryPRI ?? _undoEngine.MediaManager.AnimationDirectorySEC)
                ?.GetAllFiles();
            switch (sourceProxy.StartType)
            {
                case TStartType.After:
                    return sourceProxy.InsertAfter(_undoDest, mediaFiles, animationFiles);
                case TStartType.WithParent:
                case TStartType.WithParentFromEnd:
                    return sourceProxy.InsertUnder(_undoDest, sourceProxy.StartType == TStartType.WithParentFromEnd,
                        mediaFiles, animationFiles);
                case TStartType.OnFixedTime:
                case TStartType.Manual:
                    var newEvent = _undoDest == null
                        ? sourceProxy.InsertRoot(_undoEngine, mediaFiles, animationFiles)
                        : sourceProxy.InsertUnder(_undoDest, false, mediaFiles, animationFiles);
                    newEvent.ScheduledTime = sourceProxy.ScheduledTime.AddDays(1);
                    newEvent.Save();
                    return newEvent;
            }
            throw new InvalidOperationException($"Cannot undo: {source.EventName}");
        }

        #endregion //Undo

        public static void Copy(IEnumerable<EventPanelViewmodelBase> items)
        {
            Clipboard.Clear();
            foreach (var e in items)
                Clipboard.Add(EventProxy.FromEvent(e.Event));
            _operation = ClipboardOperation.Copy;
            _notifyClipboardChanged();
        }

        public static void Cut(IEnumerable<EventPanelViewmodelBase> items)
        {
            Clipboard.Clear();
            foreach (var e in items)
                Clipboard.Add(e.Event);
            _operation = ClipboardOperation.Cut;
            _notifyClipboardChanged();
        }

        public static IEvent Paste(EventPanelViewmodelBase destination, PasteLocation location)
        {
            var dest = destination.Event;
            if (CanPaste(destination, location))
            {
                var operation = _operation;
                using (var enumerator = Clipboard.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                        return null;
                    dest = _paste(enumerator.Current, dest, location, operation);
                    while (enumerator.MoveNext())
                        dest = _paste(enumerator.Current, dest, PasteLocation.After, operation);
                }
            }
            if (_operation == ClipboardOperation.Cut)
                Clipboard.Clear();
            return dest;
        }

        static IEvent _paste(IEventProperties source, IEvent dest, PasteLocation location, ClipboardOperation operation)
        {
            if (operation == ClipboardOperation.Cut)
            {
                if (source is IEvent sourceEvent)
                {
                    if (sourceEvent.Engine == dest.Engine)
                    {
                        sourceEvent.Remove();
                        switch (location)
                        {
                            case PasteLocation.After:
                                dest.InsertAfter(sourceEvent);
                                break;
                            case PasteLocation.Before:
                                dest.InsertBefore(sourceEvent);
                                break;
                            case PasteLocation.Under:
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
                if (source is EventProxy sourceProxy)
                {
                    var mediaFiles = (dest.Engine.MediaManager.MediaDirectoryPRI ?? dest.Engine.MediaManager.MediaDirectorySEC)?.GetAllFiles();
                    var animationFiles = (dest.Engine.MediaManager.AnimationDirectoryPRI ?? dest.Engine.MediaManager.AnimationDirectorySEC)?.GetAllFiles();
                    switch (location)
                    {
                        case PasteLocation.After:
                            return sourceProxy.InsertAfter(dest, mediaFiles, animationFiles);
                        case PasteLocation.Before:
                            return sourceProxy.InsertBefore(dest, mediaFiles, animationFiles);
                        case PasteLocation.Under:
                            var newEvent = sourceProxy.InsertUnder(dest, false, mediaFiles, animationFiles);
                            if (dest.EventType == TEventType.Container)
                                newEvent.ScheduledTime = IEventExtensions.DefaultScheduledTime;
                            return newEvent;
                    }
                    throw new InvalidOperationException("Invalid paste location");
                }
                else
                    throw new InvalidOperationException($"Cannot paste from type: {source?.GetType().Name}");
            }
        }

        public static bool CanPaste(EventPanelViewmodelBase destEventVm, PasteLocation location)
        {
            if (destEventVm?.Event == null)
                return false;
            IEventProperties dest = destEventVm.Event;
            var operation = _operation;
            var destStartType = dest.StartType;
            if (location != PasteLocation.Under 
                && (destStartType == TStartType.Manual || destStartType == TStartType.OnFixedTime) 
                && Clipboard.Any(e => e.EventType != TEventType.Rundown))
                return false;
            using (var enumerator = Clipboard.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return false;
                if (!_canPaste(enumerator.Current, dest, location, operation))
                    return false;
                dest = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    if (!_canPaste(enumerator.Current, dest, PasteLocation.After, operation))
                        return false;
                    dest = enumerator.Current;
                }
            }
            return true;
        }

        private static bool _canPaste(IEventProperties source, IEventProperties dest, PasteLocation location, ClipboardOperation operation)
        {
            var sourceEvent = source as IEvent;
            var destEvent = dest as IEvent;
            if (source == null
                || (operation == ClipboardOperation.Cut && (destEvent == null || sourceEvent?.Engine != destEvent.Engine))
                || (destEvent != null && !destEvent.HaveRight(EventRight.Create)))
                return false;
            if (location == PasteLocation.Under)
            {
                if (dest.EventType == TEventType.StillImage)
                    return false;
                if ((dest.EventType == TEventType.Movie || dest.EventType == TEventType.Live) && source.EventType != TEventType.StillImage)
                    return false;
                if (dest.EventType == TEventType.Rundown && (source.EventType == TEventType.StillImage || destEvent?.SubEventsCount > 0))
                    return false;
                if (dest.EventType == TEventType.Container && source.EventType != TEventType.Rundown)
                    return false;
            }
            if (location == PasteLocation.After || location == PasteLocation.Before)
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
