using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

        private IUiPreview _currentPreview;
        private ShuttlePositionEnum _shuttlePosition = ShuttlePositionEnum.Neutral;

        public Plugin()
        {
            EventManager.RegisterClassHandler(
                typeof(FrameworkElement),
                Keyboard.PreviewGotKeyboardFocusEvent,
                (KeyboardFocusChangedEventHandler)OnPreviewGotKeyboardFocus);
        }

        private void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!(sender is FrameworkElement element &&
                element.DataContext is IUiPreviewProvider previewProvider &&
                previewProvider.Engine == Context?.Engine
                ))
                return;
            CurrentPreview = previewProvider.Preview;
        }

        [XmlIgnore]
        public IUiPreview CurrentPreview
        {
            get => _currentPreview;
            private set
            {
                if (_currentPreview == value)
                    return;
                _currentPreview = value;
                Logger.Trace("Preview changed");
            }
        }

        [XmlAttribute]
        public string EngineName { get; set; }

        [XmlAttribute]
        public byte UnitId { get; set; }

        public IUiMenuItem Menu { get; } = null; // this plugin does not contain any menu item

        public Command[] Commands { get; set; } = new Command[0];

        public Backlight[] Backlights { get; set; } = new Backlight[0];

        [XmlIgnore]
        public IUiPluginContext Context { get; private set; }

        internal bool SetContext(IUiPluginContext context)
        {
            if (Context != null)
                return false;
            Context = context;
            context.Engine.PropertyChanged += Engine_PropertyChanged;
            XKeysDeviceEnumerator.DeviceConnected += DeviceEnumeratorOnDeviceConnected;
            SetBacklight(context.Engine);
            Logger.Debug("Preview changed");
            return true;
        }

        private void DeviceEnumeratorOnDeviceConnected(object _, XKeysDevice device)
        {
            SetBacklight(Context?.Engine);
        }

        internal void Notify(KeyNotifyEventArgs e)
        {
            try
            {
                if (e.Device.UnitId != UnitId)
                    return;
                if (!(Context is IUiEngine engine))
                    return;
                Logger.Trace("Key notified: UnitId={0}, IsPressed={1}, Key={2}, AllKeys=[{3}]", e.Device.UnitId, e.IsPressed, e.Key, string.Join(",", e.AllKeys));
                var preview = CurrentPreview;
                if (e.Device.DeviceModel == DeviceModelEnum.Xk12JogAndShuttle && e.IsPressed && preview != null)
                {
                    if (e.Key == 7)
                        preview.CommandFastForwardOneFrame.Execute(null);
                    if (e.Key == 15)
                        preview.CommandBackwardOneFrame.Execute(null);
                    SetShuttlePosition(e.Key);
                }
                var commands = Commands.Where(c =>
                    c.Key == e.Key &&
                    e.IsPressed == (c.ActiveOn == ActiveOnEnum.Press) &&
                    (c.Required < 0 || e.AllKeys.Contains(c.Required))).ToList();
                var command = commands.FirstOrDefault(c => c.Required >= 0) ?? commands.FirstOrDefault(); //executes single command, the one with modifier has higher priority
                if (command == null)
                    return;
                switch (command.CommandTarget)
                {
                    case CommandTargetEnum.Engine:
                        ExecuteOnEngine(engine, command.Method, command.Parameter);
                        break;
                    case CommandTargetEnum.Preview:
                        ExecuteOnPreview(CurrentPreview, command.Method, command.Parameter);
                        break;
                    case CommandTargetEnum.Keyboard:
                        ExecuteOnKeyboard(command.Method);
                        break;
                    case CommandTargetEnum.SelectedEvent:
                        ExecuteOnSelectedEvent(engine.SelectedEvent, command.Method);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void SetShuttlePosition(int key)
        {
            if (!Enum.IsDefined(typeof(ShuttlePositionEnum), key))
                return;
            var oldPosition = _shuttlePosition;
            _shuttlePosition = (ShuttlePositionEnum)key;
            if (_shuttlePosition == ShuttlePositionEnum.Neutral)
                return;
            if (oldPosition == ShuttlePositionEnum.Neutral)
                Shuttle();
        }

        private async void Shuttle()
        {
            var preview = CurrentPreview;
            if (preview == null)
                return;
            while (_shuttlePosition != ShuttlePositionEnum.Neutral)
            {
                await Task.Run(() =>
                {
                    preview.OnUiThread(() => preview.CommandSeek.Execute(ShuttlePositionToFrames(_shuttlePosition)));
                    Thread.Sleep(1000);
                });
            }
        }

        private static long ShuttlePositionToFrames(ShuttlePositionEnum shuttlePosition)
        {
            switch (shuttlePosition)
            {
                case ShuttlePositionEnum.Backward7:
                    return -15625;
                case ShuttlePositionEnum.Backward6:
                    return -3125;
                case ShuttlePositionEnum.Backward5:
                    return -625;
                case ShuttlePositionEnum.Backward4:
                    return -125;
                case ShuttlePositionEnum.Backward3:
                    return -25;
                case ShuttlePositionEnum.Backward2:
                    return -5;
                case ShuttlePositionEnum.Backward1:
                    return -1;
                case ShuttlePositionEnum.Neutral:
                    return 0;
                case ShuttlePositionEnum.Forward1:
                    return 1;
                case ShuttlePositionEnum.Forward2:
                    return 5;
                case ShuttlePositionEnum.Forward3:
                    return 25;
                case ShuttlePositionEnum.Forward4:
                    return 125;
                case ShuttlePositionEnum.Forward5:
                    return 625;
                case ShuttlePositionEnum.Forward6:
                    return 3125;
                case ShuttlePositionEnum.Forward7:
                    return 15625;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shuttlePosition));
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
            if (propertyInfo == null || engine == null)
                return;
            var o = propertyInfo.GetValue(engine);
            if (!(o is ICommand command))
                return;
            engine.OnUiThread(() => command.Execute(commandParameter));
        }

        private static void ExecuteOnPreview(IUiPreview preview, string commandName, string commandParameter)
        {
            var propertyName = $"Command{commandName}";
            var propertyInfo = typeof(IUiPreview).GetProperty(propertyName);
            if (propertyInfo == null || preview == null)
                return;
            var o = propertyInfo.GetValue(preview);
            if (!(o is ICommand command))
                return;
            preview.OnUiThread(() => command.Execute(commandParameter));
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

        private void SetBacklight(IEngine engine)
        {
            try
            {
                foreach (var backlight in Backlights.Where(b => b.State != engine.EngineState))
                    foreach (var backlightKey in backlight.Keys)
                        XKeysDeviceEnumerator.SetBacklight(UnitId, backlightKey, BacklightColorEnum.None, false);
                foreach (var backlight in Backlights.Where(b => b.State == engine.EngineState))
                    foreach (var backlightKey in backlight.Keys)
                        XKeysDeviceEnumerator.SetBacklight(UnitId, backlightKey, backlight.Color, backlight.Blinking);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }
        }

        private void Engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IEngine.EngineState) || !(sender is IEngine engine) || Backlights == null)
                return;
            Task.Run(() => SetBacklight(engine));
        }
    }


}
