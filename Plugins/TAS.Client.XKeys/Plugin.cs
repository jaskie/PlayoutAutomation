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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
                Logger.Trace("Key notified: UnitId={0}, IsPressed={1}, Key={2}, AllKeys=[{3}]", keyNotifyEventArgs.UnitId, keyNotifyEventArgs.IsPressed, keyNotifyEventArgs.Key, string.Join(",", keyNotifyEventArgs.AllKeys));
                if (keyNotifyEventArgs.UnitId != UnitId)
                    return;
                if (!(Context is IUiEngine engine))
                    return;
                var commands = Commands.Where(c =>
                    c.Key == keyNotifyEventArgs.Key &&
                    keyNotifyEventArgs.IsPressed == (c.ActiveOn == ActiveOnEnum.Press) &&
                    (c.Required < 0 || keyNotifyEventArgs.AllKeys.Contains(c.Required))).ToList();
                var command = commands.FirstOrDefault(c => c.Required >= 0) ?? commands.FirstOrDefault(); //executes single command, the one with modifier has higher priority
                if (command == null)
                    return;
                switch (command.CommandTarget)
                {
                    case CommandTargetEnum.Engine:
                        ExecuteOnEngine(engine, command.Method, command.Parameter);
                        break;
                    case CommandTargetEnum.Keyboard:
                        ExecuteOnKeyboard(command.Method);
                        break;
                    case CommandTargetEnum.SelectedEvent:
                        ExecuteOnSelectedEvent(engine.SelectedEvent, command.Method);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void ExecuteOnKeyboard(string method)
        {
            var gesture = (KeyGesture)KeyGestureConverter.ConvertFromString(method);
            if (gesture == null)
                return;
            var modifiers = new List<VirtualKeyCode>();
            if ((gesture.Modifiers & ModifierKeys.Alt) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.LMENU);
            if ((gesture.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.CONTROL);
            if ((gesture.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.SHIFT);
            if ((gesture.Modifiers & ModifierKeys.Windows) != ModifierKeys.None)
                modifiers.Add(VirtualKeyCode.LWIN);
            InputSimulator.Keyboard.ModifiedKeyStroke(modifiers, (VirtualKeyCode)KeyInterop.VirtualKeyFromKey(gesture.Key));
        }

        private static void ExecuteOnEngine(IUiEngine engine, string commandMethod, string commandParameter)
        {
            var propertyName = $"Command{commandMethod}";
            var propertyInfo = typeof(IUiEngine).GetProperty(propertyName);
            if (propertyInfo == null)
                return;
            var o = propertyInfo.GetValue(engine);
            if (!(o is ICommand command))
                return;
            engine.OnUiThread(() => command.Execute(commandParameter));
        }

        private static void ExecuteOnSelectedEvent(IEvent e, string commandMethod)
        {
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

            }
        }

    }
}
