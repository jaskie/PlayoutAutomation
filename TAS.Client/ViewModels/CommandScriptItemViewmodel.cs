using System;
using System.ComponentModel;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class CommandScriptItemViewmodel : OkCancelViewmodelBase<ICommandScriptItem>, ICommandScriptItem, IDataErrorInfo
    {
        public CommandScriptItemViewmodel(ICommandScriptItem item, RationalNumber frameRate):base(item, new Views.CommandScriptItemEditView(frameRate), resources._window_CommandScriptItemEditWindowTitle)
        {
            IsFinalizationCommand = item.ExecuteTime == null;
        }

        protected override void OnDispose()
        {
        }

        private TimeSpan? _executeTime;
        public TimeSpan? ExecuteTime
        {
            get { return _executeTime; }
            set
            {
                if (SetField(ref _executeTime, value, nameof(ExecuteTime)) && value != null)
                    ExecuteTimeValue = value.Value;
            }
        }

        private TimeSpan _executeTimeValue;
        public TimeSpan ExecuteTimeValue
        {
            get { return _executeTimeValue; }
            set
            {
                if (SetField(ref _executeTimeValue, value, nameof(ExecuteTimeValue)))
                    ExecuteTime = value;
            }
        }

        private string _command;
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value, nameof(Command)); }
        }

        public bool IsFinalizationCommand { get; set; }

        private bool _preview;
        public bool Preview
        {
            get { return _preview; }
            set
            {
                if (SetField(ref _preview, value, nameof(Preview)))
                {
                }
            }
        }

        public string Error { get { return String.Empty; } }

        public string this[string columnName]
        {
            get
            {
                string validationResult = null;
                switch (columnName)
                {
                    case nameof(Command):
                        if (!ValidateCommandText(Command))
                            validationResult = resources._validate_CommandSyntax;
                        break;
                }
                return validationResult;
            }
        }

        public bool ValidateCommandText(string commandText)
        {
            return Model.ValidateCommandText(commandText);
        }
        
        protected override void Ok(object o)
        {
            Window.DialogResult = true; // do not save to model yet
        }

    }
}