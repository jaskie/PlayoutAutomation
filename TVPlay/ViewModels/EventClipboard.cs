using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server;

namespace TAS.Client.ViewModels
{
    internal class EventClipboard
    {
        private readonly EngineViewmodel _engineVm;
        private List<EventViewmodel> _eventVmList = new List<EventViewmodel>();
        internal EventClipboard(EngineViewmodel engineVm)
        {
            _engineVm = engineVm;
        }
        private EventViewmodel _cutEventVm;

        internal enum TPasteLocation { Under, Before, After };

        internal enum ClipboardOperation { Cut, Copy };

        private ClipboardOperation _operation;
       
        private bool _singleEvent;

        internal void CutSingle(EventViewmodel eventVm)
        {
            _clearList();
            _singleEvent = true;
            _cutEventVm = eventVm;
            _eventVmList.Add(eventVm);
            _operation = ClipboardOperation.Cut;
            eventVm.IsCut = true;
        }

        internal void CutMultiple(EventViewmodel startEventVm)
        {
            _clearList();
            _singleEvent = false;
            _cutEventVm = startEventVm;
            _operation = ClipboardOperation.Cut;
            do
            {
                _eventVmList.Add(startEventVm);
                startEventVm.IsCut = true;
                startEventVm = startEventVm.Next;
            }
            while (startEventVm != null);
        }

        internal void CopySingle(EventViewmodel eventVm)
        {
            _clearList();
            _singleEvent = true;
            _cutEventVm = eventVm;
            _eventVmList.Add(eventVm);
            _operation = ClipboardOperation.Copy;
            eventVm.IsCopy = true;
        }

        internal void CopyMultiple(EventViewmodel startEventVm)
        {
            _clearList();
            _singleEvent = false;
            _cutEventVm = startEventVm;
            _operation = ClipboardOperation.Copy;
            do
            {
                _eventVmList.Add(startEventVm);
                startEventVm.IsCopy = true;
                startEventVm = startEventVm.Next;
            }
            while (startEventVm != null);
        }

        internal bool CanPaste(EventViewmodel destEventVm, TPasteLocation location)
        {
            bool canPaste = false;
            EventViewmodel sourceEventVm = _cutEventVm;
            if (destEventVm != null && sourceEventVm != null)
            {
                Event destEvent = destEventVm.Event;
                Event sourceEvent = sourceEventVm.Event;
                if (destEvent != null && sourceEvent != null)
                {
                    if (_operation == ClipboardOperation.Cut)
                    {

                        if (destEvent == sourceEvent)
                            return false;
                        if (location == TPasteLocation.Under)
                        {
                            if (destEvent.EventType == TEventType.StillImage)
                                return false;
                            if ((destEvent.EventType == TEventType.Movie || destEvent.EventType == TEventType.Live) && (sourceEvent.EventType != TEventType.StillImage || sourceEvent.EventType != TEventType.AnimationFlash))
                                return false;
                            if (destEvent.EventType == TEventType.Rundown && (sourceEvent.EventType == TEventType.StillImage || sourceEvent.EventType == TEventType.AnimationFlash || destEvent.SubEvents.Count > 0))
                                return false;
                            if (destEvent.EventType == TEventType.Container && sourceEvent.EventType != TEventType.Rundown)
                                return false;
                        }
                        // checkin for circular references
                        Event prev = destEvent;
                        if (_singleEvent)
                            canPaste = !sourceEvent.VisualRootTrack.Contains(destEvent);
                        else
                            while (prev != null)
                            {
                                if (prev == sourceEvent)
                                    return false;
                                prev = prev.Prior ?? prev.Parent;
                            }
                        canPaste = true;
                        if (location == TPasteLocation.After 
                            && (destEvent == sourceEvent.Prior || destEvent.EventType == TEventType.Container))
                            return false;
                        if (location == TPasteLocation.Before
                            && (destEvent == sourceEvent || destEvent.StartType == TStartType.Manual || destEvent.EventType == TEventType.Container))
                            return false;
                        if (location == TPasteLocation.Under && destEvent == sourceEvent.Parent)
                            return false;
                    }

                    if (_operation == ClipboardOperation.Copy)
                    {
                        if (location == TPasteLocation.Before || location == TPasteLocation.After)
                            return destEvent.EventType != TEventType.Container;
                        if (location == TPasteLocation.Under)
                        {
                            if (destEvent.EventType == TEventType.StillImage)
                                return false;
                            if ((destEvent.EventType == TEventType.Movie || destEvent.EventType == TEventType.Live) && (sourceEvent.EventType != TEventType.StillImage || sourceEvent.EventType != TEventType.AnimationFlash))
                                return false;
                            if (destEvent.EventType == TEventType.Rundown && (sourceEvent.EventType == TEventType.StillImage || sourceEvent.EventType == TEventType.AnimationFlash || destEvent.SubEvents.Count > 0))
                                return false;
                            if (destEvent.EventType == TEventType.Container && sourceEvent.EventType == TEventType.Rundown)
                                return true;
                        }
                    }
                }
            }
            return canPaste;
        }

        internal void Paste(EventViewmodel destEventVm, TPasteLocation location)
        {
            if (!CanPaste(destEventVm, location))
                return;
            
            Event dest = destEventVm.Event;
            Event cutFirst = _cutEventVm.Event;
            if (_operation == ClipboardOperation.Cut)
            {
                if (_singleEvent)
                    cutFirst.Remove();
                switch (location)
                {
                    case TPasteLocation.After:
                        dest.InsertAfter(cutFirst);
                        break;
                    case TPasteLocation.Before:
                        dest.InsertBefore(cutFirst);
                        break;
                    case TPasteLocation.Under:
                        dest.InsertUnder(cutFirst);
                        break;
                }
                cutFirst = cutFirst.Next;
                while (cutFirst != null)
                {
                    cutFirst.Save();
                    cutFirst = cutFirst.Next;
                }
                _clearList();
            }
         
            if (_operation == ClipboardOperation.Copy)
            {
                Event newEvent = cutFirst.Clone();
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
                if (!_singleEvent)
                {
                    cutFirst = cutFirst.Next;
                    while (cutFirst != null)
                    {
                        newEvent.InsertAfter(cutFirst.Clone());
                        cutFirst = cutFirst.Next;
                    }
                }
                // don't clear copy items list
            }
        }

        private void _clearList()
        {
            foreach (EventViewmodel evm in _eventVmList)
            {
                evm.IsCut = false;
                evm.IsCopy = false;
            }
            _eventVmList.Clear();
            _cutEventVm = null;
        }

    }
}
