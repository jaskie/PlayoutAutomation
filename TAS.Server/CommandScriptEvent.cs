using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        public CommandScriptEvent(Engine engine, ulong idRundownEvent, ulong idEventBinding, TPlayState playState, DateTime scheduledTime, TimeSpan duration, TimeSpan scheduledDelay, string eventName, DateTime startTime, bool isEnabled, IEnumerable<ICommandScriptItem> commands) 
            : base(engine, idRundownEvent, idEventBinding, VideoLayer.None, TEventType.CommandScript, TStartType.With, playState, scheduledTime, duration, scheduledDelay, TimeSpan.Zero, Guid.Empty, eventName, startTime, TimeSpan.Zero, null, TimeSpan.Zero, TimeSpan.Zero, TTransitionType.Cut, TEasing.None, null, 0, string.Empty, isEnabled, false, false, default(EventGPI), AutoStartFlags.None)
        {
            _commands = new List<CommandScriptItem>(commands.Select(i => new CommandScriptItem { ExecuteTime = i.ExecuteTime, Command = i.Command }));
        }

        readonly object _commandsSyncRoot = new object();
        readonly List<CommandScriptItem> _commands;

        public IList<ICommandScriptItem> Commands
        {
            get
            {
                lock (_commandsSyncRoot)
                    return _commands.Cast<ICommandScriptItem>().ToList();
            }
            set
            {
                lock(_commandsSyncRoot)
                {
                    _commands.Clear();
                    _commands.AddRange(value.Select(i => new CommandScriptItem { ExecuteTime = i.ExecuteTime, Command = i.Command }));
                }
                NotifyPropertyChanged(nameof(Commands));
            }
        }

        public ICommandScriptItem AddCommand(TimeSpan? executeTime, string command)
        {
            var newCommand =  new CommandScriptItem { ExecuteTime = executeTime, Command = command };
            lock (_commandsSyncRoot)
                _commands.Add(newCommand);
            return newCommand;
        }

        public void AddCommand(ICommandScriptItem command)
        {
            lock(_commandsSyncRoot)
                _commands.Add(command as CommandScriptItem);
        }

        public bool DeleteCommand(ICommandScriptItem command)
        {
            lock(_commandsSyncRoot)
                return _commands.Remove(command as CommandScriptItem);
        }

        internal IEnumerable<ICommandScriptItem> EndCommands
        {
            get
            {
                lock (_commandsSyncRoot)
                    return _commands.Where(c => c.ExecuteTime == null).ToArray();
            }
        }

        public IEnumerable<ICommandScriptItem> ItemsToExecute
        {
            get
            {
                lock (_commandsSyncRoot)
                    return _commands.Where(c => !c.IsExecuted && c.ExecuteTime?.ToSMPTEFrames(Engine.FrameRate) >= Position).ToArray();
            }
        }

        protected override bool SetPlayState(TPlayState newPlayState)
        {
            if (base.SetPlayState(newPlayState))
            {
                if (newPlayState == TPlayState.Scheduled)
                    lock (_commandsSyncRoot)
                        _commands.ForEach(i => i.IsExecuted = false);
                return true;
            }
            return false;
        }
    }
}
