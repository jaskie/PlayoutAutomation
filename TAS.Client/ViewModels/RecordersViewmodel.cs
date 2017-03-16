using System;
using System.Collections.Generic;
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
    public class RecordersViewmodel : ViewmodelBase
    {
        private readonly IEnumerable<IRecorder> _recorders;
        private readonly IServerDirectory _directory;
        public RecordersViewmodel(IEnumerable<IRecorder> recorders, IServerDirectory directory)
        {
            _createCommands();
            _directory = directory;
            _recorders = recorders;
            Recorder = _recorders.FirstOrDefault();
        }

        private void _createCommands()
        {
            CommandAbort = new UICommand { ExecuteDelegate = _abort, CanExecuteDelegate = _canAbort };
            CommandFastForward = new UICommand { ExecuteDelegate = _fastForward, CanExecuteDelegate = _canControlDeck };
            CommandPlay = new UICommand { ExecuteDelegate = _play, CanExecuteDelegate = _canControlDeck };
            CommandStop = new UICommand { ExecuteDelegate = _stop, CanExecuteDelegate = _canControlDeck };
            CommandCapture = new UICommand { ExecuteDelegate = _capture, CanExecuteDelegate = _canCapture };
        }

        private void _capture(object obj)
        {
            _recorder?.Capture(_channel, _tcIn, _tcOut, $"{FileName}.{_fileFormat}");
        }

        private bool _canCapture(object obj)
        {
            return _recorder != null && _channel != null && _tcOut > _tcIn && this[nameof(FileName)] == null;
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

        private bool _canControlDeck(object obj)
        {
            throw new NotImplementedException();
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
                    oldRecorder.PropertyChanged -= _recorder_PropertyChanged;
                    value.PropertyChanged += _recorder_PropertyChanged;
                    Channel = value.Channels.LastOrDefault();
                }
            }
        }

        public IEnumerable<IRecorder> Recorders { get { return _recorders; } }

        private IPlayoutServerChannel _channel;
        public IPlayoutServerChannel Channel { get { return _channel; }  set { SetField(ref _channel, value, nameof(Channel)); } }

        private TMovieContainerFormat _fileFormat;
        public TMovieContainerFormat FileFormat { get { return _fileFormat; } set { SetField(ref _fileFormat, value, nameof(FileFormat)); } }

        private TimeSpan _tcIn;
        public TimeSpan TcIn { get { return _tcIn; } set { SetField(ref _tcIn, value, nameof(TcIn)); } }

        private TimeSpan _tcOut;
        public TimeSpan TcOut { get { return _tcOut; } set { SetField(ref _tcOut, value, nameof(TcOut)); } }

        private TimeSpan _currentTc;
        public TimeSpan CurrentTc { get { return _currentTc; }  private set { SetField(ref _currentTc, value, nameof(CurrentTc)); } }

        private TDeckState _deckState;
        public TDeckState DeckState { get { return _deckState; } private set { SetField(ref _deckState, value, nameof(DeckState)); } }

        private TDeckControl _deckControl;
        public TDeckControl DeckControl { get { return _deckControl; }  private set { SetField(ref _deckControl, value, nameof(DeckControl)); } }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(FileName):
                        validationResult = _validateFileName();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateFileName()
        {
            string validationResult = string.Empty;
            string newName = $"{_fileName}.{_fileFormat}";
            if (_fileName != null)
            {
                if (newName.StartsWith(" ") || newName.EndsWith(" "))
                    validationResult = resources._validate_FileNameCanNotStartOrEndWithSpace;
                else
                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                    validationResult = resources._validate_FileNameCanNotContainSpecialCharacters;
                else
                {
                    newName = newName.ToLowerInvariant();
                    if (_directory.FileExists(newName))
                        validationResult = resources._validate_FileAlreadyExists;
                }
            }
            return validationResult;
        }

        private void _recorder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRecorder.CurrentTc))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => CurrentTc = ((IRecorder)sender).CurrentTc));
            if (e.PropertyName == nameof(IRecorder.DeckControl))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => DeckControl = ((IRecorder)sender).DeckControl));
            if (e.PropertyName == nameof(IRecorder.DeckState))
                Application.Current.Dispatcher.BeginInvoke((Action)(() => DeckState = ((IRecorder)sender).DeckState));
        }

        protected override void OnDispose()
        {
            _recorder = null; // ensure that event is disconnected
        }
    }
}
