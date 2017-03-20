﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class RecordersViewmodel : ViewmodelBase, IDataErrorInfo
    {
        private readonly IEnumerable<IRecorder> _recorders;
        public RecordersViewmodel(IEnumerable<IRecorder> recorders)
        {
            _createCommands();
            _recorders = recorders;
            Recorder = _recorders.FirstOrDefault();
        }

        private void _createCommands()
        {
            CommandAbort = new UICommand { ExecuteDelegate = _abort, CanExecuteDelegate = _canAbort };
            CommandFastForward = new UICommand { ExecuteDelegate = _fastForward, CanExecuteDelegate = o => _canExecute(TDeckState.ShuttleForward) };
            CommandRewind = new UICommand { ExecuteDelegate = _rewind, CanExecuteDelegate = _canRewind };
            CommandPlay = new UICommand { ExecuteDelegate = _play, CanExecuteDelegate = o => _canExecute(TDeckState.Playing) };
            CommandStop = new UICommand { ExecuteDelegate = _stop, CanExecuteDelegate = o => _canExecute(TDeckState.Stopped) };
            CommandCapture = new UICommand { ExecuteDelegate = _capture, CanExecuteDelegate = _canCapture };
            CommandGetCurrentTcToIn = new UICommand { ExecuteDelegate = o => TcIn = CurrentTc };
            CommandGetCurrentTcToOut = new UICommand { ExecuteDelegate = o => TcOut = CurrentTc };
            CommandGoToTimecode = new UICommand { ExecuteDelegate = _goToTimecode, CanExecuteDelegate = _canGoToTimecode };
        }

        private void _goToTimecode(object obj)
        {
            _recorder.GoToTimecode(_currentTc, _channel.VideoFormat);
        }

        private bool _canGoToTimecode(object obj)
        {
            IRecorder recorder = _recorder;
            return recorder != null && recorder.IsConnected && _channel != null;
        }

        private void _rewind(object obj)
        {
            _recorder?.Rewind();
        }

        private bool _canRewind(object obj)
        {
            return _canExecute(TDeckState.ShuttleForward);
        }

        private void _capture(object obj)
        {
            if (_recorder != null)
            {
                _recorder.Capture(_channel, _tcIn, _tcOut, $"{FileName}.{FileFormat}");
            }
        }

        private bool _canCapture(object obj)
        {
            return _recorder != null && _channel != null && _tcOut > _tcIn && _recorder.IsConnected && string.IsNullOrEmpty(_validateFileName());
        }

        private bool _canExecute(TDeckState state)
        {
            IRecorder recorder = _recorder;
            return recorder != null && recorder.IsConnected && recorder.DeckState != state;
        }

        private void _stop(object obj)
        {
            _recorder?.Stop();
        }

        private void _play(object obj)
        {
            _recorder?.Play();
        }

        private void _fastForward(object obj)
        {
            _recorder?.FastForward();
        }

        private void _abort(object obj)
        {
            _recorder?.Abort();
        }

        private bool _canAbort(object obj)
        {
            throw new NotImplementedException();
        }

        public ICommand CommandPlay { get; private set; }
        public ICommand CommandStop { get; private set; }
        public ICommand CommandFastForward { get; private set; }
        public ICommand CommandRewind { get; private set; }
        public ICommand CommandAbort { get; private set; }
        public ICommand CommandCapture { get; private set; }
        public ICommand CommandGetCurrentTcToIn { get; private set; }
        public ICommand CommandGetCurrentTcToOut { get; private set; }
        public ICommand CommandGoToTimecode { get; private set; }
        public ICommand CommandRecordEnd { get; private set; }
        public ICommand CommandRecordStart { get; private set; }
        public ICommand CommandSetRecordLimit { get; private set; }

        private string _fileName;
        public string FileName { get { return _fileName; } set { SetField(ref _fileName, value, nameof(FileName)); } }

        private IRecorder _recorder;
        public IRecorder Recorder
        {
            get { return _recorder; }
            set
            {
                var oldRecorder = _recorder;
                if (SetField(ref _recorder, value, nameof(Recorder)))
                {
                    if (oldRecorder != null)
                        oldRecorder.PropertyChanged -= _recorder_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += _recorder_PropertyChanged;
                    Channels = value.Channels;
                    Channel = value.Channels.LastOrDefault();
                }
            }
        }

        public IEnumerable<IRecorder> Recorders { get { return _recorders; } }

        private IEnumerable<IPlayoutServerChannel> _channels;
        public IEnumerable<IPlayoutServerChannel> Channels { get { return _channels; } private set { SetField(ref _channels, value, nameof(Channels)); } }

        private IPlayoutServerChannel _channel;
        public IPlayoutServerChannel Channel { get { return _channel; }  set { SetField(ref _channel, value, nameof(Channel)); } }

        public Array FileFormats { get { return Enum.GetValues(typeof(TMovieContainerFormat)); } }

        private TMovieContainerFormat _fileFormat;
        public TMovieContainerFormat FileFormat { get { return _fileFormat; } set { SetField(ref _fileFormat, value, nameof(FileFormat)); } }

        private TimeSpan _tcIn;
        public TimeSpan TcIn { get { return _tcIn; } set { SetField(ref _tcIn, value, nameof(TcIn)); } }

        private TimeSpan _tcOut;
        public TimeSpan TcOut { get { return _tcOut; } set { SetField(ref _tcOut, value, nameof(TcOut)); } }

        private TimeSpan _currentTc;
        public TimeSpan CurrentTc { get { return _currentTc; }  set { SetField(ref _currentTc, value, nameof(CurrentTc)); } }

        private TimeSpan _recorderTimeLimit;
        public TimeSpan RecorderTimeLimit
        {
            get { return _recorderTimeLimit; }
            set
            {
                if (SetField(ref _recorderTimeLimit, value, nameof(RecorderTimeLimit)))
                    Debug.WriteLine(value);
            }
        }

        private TDeckState _deckState;
        public TDeckState DeckState
        {
            get { return _deckState; }
            private set
            {
                if (SetField(ref _deckState, value, nameof(DeckState)))
                    InvalidateRequerySuggested();
            }
        }

        private TDeckControl _deckControl;
        public TDeckControl DeckControl
        {
            get { return _deckControl; }
            private set
            {
                if (SetField(ref _deckControl, value, nameof(DeckControl)))
                    InvalidateRequerySuggested();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(FileName):
                    case nameof(FileFormat):
                        validationResult = _validateFileName();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateFileName()
        {
            if (string.IsNullOrWhiteSpace(_fileName))
                return resources._validate_FileNameEmpty;
            string newName = $"{_fileName}.{_fileFormat}";
            if (newName.StartsWith(" ") || newName.EndsWith(" "))
                return resources._validate_FileNameCanNotStartOrEndWithSpace;
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                return resources._validate_FileNameCanNotContainSpecialCharacters;
            if (_recorder?.RecordingDirectory.FileExists(newName) == true)
                    return resources._validate_FileAlreadyExists;
            return string.Empty;
        }

        public string Error
        {
            get { return null; }
        }

        private void _recorder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRecorder.CurrentTc))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => CurrentTc = ((IRecorder)sender).CurrentTc));
            if (e.PropertyName == nameof(IRecorder.DeckControl))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => DeckControl = ((IRecorder)sender).DeckControl));
            if (e.PropertyName == nameof(IRecorder.DeckState))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => DeckState = ((IRecorder)sender).DeckState));
            if (e.PropertyName == nameof(IRecorder.IsConnected))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => InvalidateRequerySuggested()));
        }

        protected override void OnDispose()
        {
            Recorder = null; // ensure that events are disconnected
        }
    }
}
