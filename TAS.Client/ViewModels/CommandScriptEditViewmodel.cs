using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Views;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CommandScriptEditViewmodel : EditViewmodelBase<ICommandScript>
    {
        private readonly IEvent _event;
        public CommandScriptEditViewmodel(IEvent aEvent, ICommandScript model) : base(model, new CommandScriptEditView())
        {
            _event = aEvent;
            CommandAddCommandScriptItem = new UICommand { ExecuteDelegate = _addCommandScriptItem, CanExecuteDelegate = _canAddCommandScriptItem };
            CommandDeleteCommandScriptItem = new UICommand { ExecuteDelegate = _deleteCommandScriptItem, CanExecuteDelegate = _canDeleteCommandScriptItem };
            CommandEditCommandScriptItem = new UICommand { ExecuteDelegate = _editCommandScriptItem, CanExecuteDelegate = _canEditCommandScriptItem };
            _commands.CollectionChanged += _commands_CollectionChanged;
        }

        private void _commands_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
        }

        protected override void OnDispose()
        {
            
        }

        private readonly ObservableCollection<CommandScriptItemViewmodel> _commands = new ObservableCollection<CommandScriptItemViewmodel>();
        public ObservableCollection<CommandScriptItemViewmodel> Commands { get { return _commands; } }
        public ICommand CommandEditCommandScriptItem { get; private set; }
        public ICommand CommandAddCommandScriptItem { get; private set; }
        public ICommand CommandDeleteCommandScriptItem { get; private set; }

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
            throw new NotImplementedException();
        }


    }
}
