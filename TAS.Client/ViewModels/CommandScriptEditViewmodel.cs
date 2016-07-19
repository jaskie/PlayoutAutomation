using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Views;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CommandScriptEditViewmodel : EditViewmodelBase<ICommandScript>
    {
        private readonly IEvent _event;
        private readonly RationalNumber _frameRate;
        public CommandScriptEditViewmodel(IEvent aEvent, ICommandScript model) : base(model, new CommandScriptEditView())
        {
            _event = aEvent;
            _frameRate = _event.Engine.FrameRate;
            CommandAddCommandScriptItem = new UICommand { ExecuteDelegate = _addCommandScriptItem, CanExecuteDelegate = _canAddCommandScriptItem };
            CommandAddEndCommandScriptItem = new UICommand { ExecuteDelegate = _addEndCommandScriptItem, CanExecuteDelegate = _canAddCommandScriptItem };
            CommandDeleteCommandScriptItem = new UICommand { ExecuteDelegate = _deleteCommandScriptItem, CanExecuteDelegate = _canDeleteCommandScriptItem };
            CommandEditCommandScriptItem = new UICommand { ExecuteDelegate = _editCommandScriptItem, CanExecuteDelegate = _canEditCommandScriptItem };
            _commands = new ObservableCollection<CommandScriptItemViewmodel>(model.Commands.Select(csi => new CommandScriptItemViewmodel(csi, _frameRate)));
            _commands.CollectionChanged += _commands_CollectionChanged;
        }

        private void _commands_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
            InvalidateRequerySuggested();
        }

        public override void Save(object destObject = null)
        {
            Model.Commands = Commands.Select(csivm => csivm.Model).ToList();
        }

        protected override void OnDispose()
        {
            
        }

        private CommandScriptItemViewmodel _selectedCommand;
        public CommandScriptItemViewmodel SelectedCommand { get { return _selectedCommand; }
        set
            {
                if (_selectedCommand != value)
                {
                    _selectedCommand = value;
                    InvalidateRequerySuggested();
                }
            }
        }

        private readonly ObservableCollection<CommandScriptItemViewmodel> _commands;
        public ObservableCollection<CommandScriptItemViewmodel> Commands
        {
            get { return _commands; }
        }
        public ICommand CommandEditCommandScriptItem { get; private set; }
        public ICommand CommandAddCommandScriptItem { get; private set; }
        public ICommand CommandDeleteCommandScriptItem { get; private set; }
        public ICommand CommandAddEndCommandScriptItem { get; private set; }

        public object SelectedCommandScriptItem { get; set; }

        private bool _canEditCommandScriptItem(object obj)
        {
            return SelectedCommandScriptItem != null;
        }

        private void _editCommandScriptItem(object obj)
        {
            throw new NotImplementedException();
        }

        private bool _canDeleteCommandScriptItem(object obj)
        {
            return SelectedCommandScriptItem != null;
        }

        private void _deleteCommandScriptItem(object obj)
        {
            throw new NotImplementedException();
        }

        private bool _canAddCommandScriptItem(object obj)
        {
            return true;
        }
        
        private void _addCommandScriptItem(object obj)
        {
            CommandScriptItemViewmodel newItem = new CommandScriptItemViewmodel(new CommandScriptItemBase(){ ExecuteTime = TimeSpan.Zero }, _frameRate);
            if (newItem.ShowDialog() == true)
            {
                if (newItem.ExecuteTime == null)
                    newItem.ExecuteTime = TimeSpan.Zero;
                _commands.Add(newItem);
            }
        }

        private void _addEndCommandScriptItem(object obj)
        {
            CommandScriptItemViewmodel newItem = new CommandScriptItemViewmodel(new CommandScriptItemBase() { ExecuteTime = null }, _frameRate) { IsFinalizationCommand = true };
            if (newItem.ShowDialog() == true)
                _commands.Add(newItem);
        }

    }
}
