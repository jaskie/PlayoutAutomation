using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client
{
    struct eventSignature
    {
        public eventSignature(IEvent aEvent)
        {
            Engine = aEvent.Engine.EngineName;
            EventId = aEvent.Id;
        }
        public string Engine;
        public ulong EventId;
    }

    public static class HiddenEventsStorage
    {
        private static readonly string _fileName = Path.Combine(FileUtils.LOCAL_APPLICATION_DATA_PATH, "HiddenEvents.json");
        static HashSet<eventSignature> _disabledEvents = new HashSet<eventSignature>();
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
                    eventSignature[] events = (eventSignature[])serializer.Deserialize(file, typeof(eventSignature[]));
                    _disabledEvents = new HashSet<eventSignature>(events);
                }
        }

        static void Save()
        {
            using (StreamWriter file = File.CreateText(_fileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, _disabledEvents.ToArray());
                }
        }

        public static void Add(IEvent aEvent)
        {
            _disabledEvents.Add(new eventSignature(aEvent));
            Save();
        }

        public static void Remove(IEvent aEvent)
        {
            _disabledEvents.Remove(new eventSignature(aEvent));
            Save();
        }


        public static bool Contains(IEvent aEvent)
        {
            return _disabledEvents.Contains(new eventSignature(aEvent));
        }
    }
}
