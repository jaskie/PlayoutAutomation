using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace TAS.Server
{
    public class CustomCommand : INotifyPropertyChanged
    {
        private readonly Engine _engine;
        public CustomCommand(Engine engine)
        {
            _engine = engine;
        }

        private string _commandIn;
        private string _commandOut;
        private string _commandName;
        internal UInt64 _idCustomCommand;

        public string CommandName
        {
            get { return _commandName; }
            set { SetField(ref _commandName, value, "CommandName"); }
        }

        public string CommandIn
        {
            get { return _commandIn; }
            set { SetField(ref _commandIn, value, "CommandIn"); }
        }

        public string CommandOut
        {
            get { return _commandOut; }
            set { SetField(ref _commandOut, value, "CommandOut"); }
        }
        
        public override string ToString()
        {
            return (!string.IsNullOrEmpty(CommandName)) ? CommandName: string.Empty;
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            lock (this)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
            }
            NotifyPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
