using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using NLog;
using TAS.Client.Common.Plugin;
using TAS.Client.XKeys.InputSimulator.Native;
using TAS.Common.Interfaces;

namespace TAS.Client.XKeys
{
    public class Plugin : IUiPlugin
    {
        private static readonly Logger Logger = LogManager.GetLogger(nameof(Plugin));
        private static readonly InputSimulator.InputSimulator InputSimulator = new InputSimulator.InputSimulator();
        private static readonly KeyGestureConverter KeyGestureConverter = new KeyGestureConverter();

        [XmlAttribute]
        public string EngineName { get; set; }

        [XmlAttribute]
        public byte UnitId { get; set; }

        public IUiMenuItem Menu { get; } = null;

        public Command[] Commands { get; set; }

        [XmlIgnore]
        public IUiPluginContext Context { get; internal set; }

        public void Notify(KeyNotifyEventArgs keyNotifyEventArgs)
        {
            try
            {
                if (keyNotifyEventArgs.UnitId != UnitId)
                    return;
                var command = Commands.FirstOrDefault(c =>
                    c.Key == keyNotifyEventArgs.Key &&
                    keyNotifyEventArgs.IsPressed == (c.ActiveOn == ActiveOnEnum.Press));
                if (command == null)
                    return;
                switch (command.CommandTarget)
                {
                    case CommandTargetEnum.Engine:
                        ExecuteOnEngine(command.Method);
                        break;
                    case CommandTargetEnum.Keyboard:
                        ExecuteOnKeyboard(command.Method);
                        break;
                    case CommandTargetEnum.SelectedEvent:
                        ExecuteOnSelectedEvent(command.Method);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void ExecuteOnKeyboard(string method)
        {
            
            var gesture = (KeyGesture)KeyGestureConverter.ConvertFromString(method);
            if (gesture == null)
                return;
            var modifiers = new List<VirtualKeyCode>();
            if ((gesture.Modifiers & ModifierKeys.Alt) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.MENU);
            if ((gesture.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.CONTROL);
            if ((gesture.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.SHIFT);
            if ((gesture.Modifiers & ModifierKeys.Windows) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.LWIN);
            InputSimulator.Keyboard.ModifiedKeyStroke(modifiers, (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(gesture.Key));
        }

        private void ExecuteOnEngine(string commandMethod)
        {
            switch (commandMethod)
            {
                case nameof(IEngine.Load):
                    Context.Engine.Load(Context.SelectedEvent);
                    break;
                case nameof(IEngine.StartLoaded):
                    Context.Engine.StartLoaded();
                    break;
                case nameof(IEngine.Clear):
                    Context.Engine.Clear();
                    break;
            }
        }

        private void ExecuteOnSelectedEvent(string commandMethod)
        {
            var e = Context.SelectedEvent;
            if (e == null)
                return;
            switch (commandMethod)
            {
                case nameof(IEvent.IsHold):
                    e.IsHold = !e.IsHold;
                    e.Save();
                    break;
                case nameof(IEvent.IsEnabled):
                    e.IsEnabled = !e.IsEnabled;
                    e.Save();
                    break;
                case nameof(IEngine.ReSchedule):
                    Context.Engine.ReSchedule(e);
                    break;
            }
        }

    }
}
