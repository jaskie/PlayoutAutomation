using Newtonsoft.Json;
using System;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        private string _command;

        internal CommandScriptEvent(Engine engine, ulong idRundownEvent, ulong idEventBinding, TStartType startType, TPlayState playState, TimeSpan scheduledDelay, string eventName, DateTime startTime, bool isEnabled, string command) 
            : base(engine, idRundownEvent, idEventBinding, VideoLayer.None, TEventType.CommandScript, startType, playState, DateTime.MinValue, TimeSpan.Zero, scheduledDelay, TimeSpan.Zero, Guid.Empty, eventName, startTime, TimeSpan.Zero, null, TimeSpan.Zero, TimeSpan.Zero, TTransitionType.Cut, TEasing.None, null, 0, string.Empty, isEnabled, false, false, AutoStartFlags.None, false, 0, 0, 0, -1, null)
        {
            _command = command;
        }

        [JsonProperty]
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value); }
        }
    }
}
