using jNet.RPC;
using System;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        private string _command;

        internal CommandScriptEvent(Engine engine, ulong idRundownEvent, ulong idEventBinding, TStartType startType, TPlayState playState, TimeSpan scheduledDelay, string eventName, DateTime startTime, bool isEnabled, string command)
            : base(engine: engine, idRundownEvent: idRundownEvent, idEventBinding: idEventBinding, videoLayer: VideoLayer.None, eventType: TEventType.CommandScript, startType: startType, playState: playState, scheduledTime: DateTime.MinValue,
                  duration: TimeSpan.Zero, scheduledDelay: scheduledDelay, scheduledTC: TimeSpan.Zero, mediaGuid: Guid.Empty, eventName: eventName, startTime: startTime, startTC: TimeSpan.Zero, requestedStartTime: null,
                  transitionTime: TimeSpan.Zero, transitionPauseTime: TimeSpan.Zero, transitionType: TTransitionType.Cut, transitionEasing: TEasing.None, audioVolume: null, idProgramme: 0, idAux: string.Empty, isEnabled: isEnabled,
                  isHold: false, isLoop: false, autoStartFlags: AutoStartFlags.None, isCGEnabled: false, crawl: 0, logo: 0, parental: 0, routerPort: -1, recordingInfo: null, signalId: 0)
        {
            _command = command;
        }

        [DtoMember]
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value); }
        }
    }
}
