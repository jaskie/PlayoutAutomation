using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client
{
    internal struct EventSignature
    {
        public EventSignature(IEvent aEvent)
        {
            Engine = aEvent.Engine.EngineName;
            EventId = aEvent.Id;
        }
        public string Engine;
        public ulong EventId;
    }

    public static class HiddenEventsStorage
    {
        private static readonly string _fileName = Path.Combine(FileUtils.LocalApplicationDataPath, "HiddenEvents.json");
        static HashSet<EventSignature> _disabledEvents = new HashSet<EventSignature>();
        static HiddenEventsStorage()
        {
            Load();
        }

        static void Load()
        {
            if (File.Exists(_fileName))
                using (StreamReader file = File.OpenText(_fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    EventSignature[] events = (EventSignature[])serializer.Deserialize(file, typeof(EventSignature[]));
                    _disabledEvents = new HashSet<EventSignature>(events);
                }
        }

        static void Save()
        {
            FileUtils.CreateDirectoryIfNotExists(FileUtils.LocalApplicationDataPath);
            using (StreamWriter file = File.CreateText(_fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, _disabledEvents.ToArray());
                }
        }

        public static void Add(IEvent aEvent)
        {
            _disabledEvents.Add(new EventSignature(aEvent));
            Save();
        }

        public static void Remove(IEvent aEvent)
        {
            _disabledEvents.Remove(new EventSignature(aEvent));
            Save();
        }


        public static bool Contains(IEvent aEvent)
        {
            return _disabledEvents.Contains(new EventSignature(aEvent));
        }
    }
}
