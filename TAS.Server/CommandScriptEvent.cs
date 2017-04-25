using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        internal CommandScriptEvent(Engine engine, ulong idRundownEvent, ulong idEventBinding, TStartType startType, TPlayState playState, TimeSpan scheduledDelay, string eventName, DateTime startTime, bool isEnabled, string command) 
            : base(engine, idRundownEvent, idEventBinding, VideoLayer.None, TEventType.CommandScript, startType, playState, DateTime.MinValue, TimeSpan.Zero, scheduledDelay, TimeSpan.Zero, Guid.Empty, eventName, startTime, TimeSpan.Zero, null, TimeSpan.Zero, TimeSpan.Zero, TTransitionType.Cut, TEasing.None, null, 0, string.Empty, isEnabled, false, false, AutoStartFlags.None, false, 0, 0, 0)
        {
            _command = command;
        }

        string _command;

        [JsonProperty]
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value); }
        }
    }
}
