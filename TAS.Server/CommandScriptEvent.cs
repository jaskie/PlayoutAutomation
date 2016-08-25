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
        public CommandScriptEvent(Engine engine, ulong idRundownEvent, ulong idEventBinding, TPlayState playState, DateTime scheduledTime, TimeSpan duration, TimeSpan scheduledDelay, string eventName, DateTime startTime, bool isEnabled, IEnumerable<ICommandScriptItem> commands) 
            : base(engine, idRundownEvent, idEventBinding, VideoLayer.None, TEventType.CommandScript, TStartType.With, playState, scheduledTime, duration, scheduledDelay, TimeSpan.Zero, Guid.Empty, eventName, startTime, TimeSpan.Zero, null, TimeSpan.Zero, TimeSpan.Zero, TTransitionType.Cut, TEasing.None, null, 0, string.Empty, isEnabled, false, false, default(EventGPI), AutoStartFlags.None)
        {
            _commands = commands == null ?
                new List<CommandScriptItem>() :
                new List<CommandScriptItem>(commands.Select(i => new CommandScriptItem { ExecuteTime = i.ExecuteTime, Command = i.Command }));
        }

        readonly object _commandsSyncRoot = new object();
        readonly List<CommandScriptItem> _commands;

        public IEnumerable<ICommandScriptItem> Commands
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
                    _commands.ToList().ForEach((c) => DeleteCommand(c));
                    foreach (var c in value)
                        AddCommand(c);
                }
                IsModified = true;
                NotifyPropertyChanged(nameof(Commands));
            }
        }

        private void _command_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsModified = true;
        }

        public ICommandScriptItem AddCommand(TimeSpan? executeTime, string command)
        {
            var newCommand =  new CommandScriptItem { ExecuteTime = executeTime, Command = command };
            lock (_commandsSyncRoot)
            {
                _commands.Add(newCommand);
                newCommand.PropertyChanged += _command_PropertyChanged;
            }  
            return newCommand;
        }

        public void AddCommand(ICommandScriptItem command)
        {
            CommandScriptItem newCommand = new CommandScriptItem { ExecuteTime = command.ExecuteTime, Command = command.Command };
            lock (_commandsSyncRoot)
            {
                _commands.Add(newCommand);
                newCommand.PropertyChanged += _command_PropertyChanged;
            }
        }

        public bool DeleteCommand(ICommandScriptItem command)
        {
            if (command is CommandScriptItem)
                lock (_commandsSyncRoot)
                {
                    if (_commands.Remove((CommandScriptItem)command))
                    {
                        ((CommandScriptItem)command).PropertyChanged -= _command_PropertyChanged;
                        return true;
                    }
                }
            else
                if (command is CommandScriptItemBase)
            {
                lock (_commandsSyncRoot)
                {
                    var c = _commands.Find((item) => item.DtoGuid == ((CommandScriptItemBase)command).DtoGuid);
                    if (c != null && _commands.Remove(c))
                    {
                        c.PropertyChanged -= _command_PropertyChanged;
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<ICommandScriptItem> ItemsToExecute
        {
            get
            {
                lock (_commandsSyncRoot)
                    if (IsFinished)
                        return _commands.Where(c => c.ExecuteTime == null).ToArray(); 
                    else
                        return _commands.Where(c => !c.IsExecuted && Position >= c.ExecuteTime?.ToSMPTEFrames(Engine.FrameRate)).ToArray();
            }
        }

        protected override bool SetPlayState(TPlayState newPlayState)
        {
            if (base.SetPlayState(newPlayState))
            {
                if (newPlayState != TPlayState.Played)
                    lock (_commandsSyncRoot)
                        _commands.ForEach(i => i.IsExecuted = false);
                return true;
            }
            return false;
        }
    }
}
