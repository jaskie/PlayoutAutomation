using Svt.Caspar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server
{

    public class CasparRecorder: DtoBase, IRecorder
    {
        internal CasparServer ownerServer;
        private TVideoFormat _tcFormat = TVideoFormat.PAL;
        private Recorder _recorder;
        internal void SetRecorder(Recorder value)
        {
            var oldRecorder = _recorder;
            if (_recorder != value)
            {
                _recorder = value;
                value.Tc += _recorder_Tc;
            }
        }

        private void _recorder_Tc(object sender, TcEventArgs e)
        {
            CurrentTc = e.Tc.SMPTETimecodeToTimeSpan(_tcFormat);
        }
        #region Deserialized properties
        public int RecorderNumber { get; set; }
        public int Id { get; set; }
        public string RecorderName { get; set; }
        #endregion Deserialized properties

        private TimeSpan _currentTc;
        [XmlIgnore]
        public TimeSpan CurrentTc { get { return _currentTc; }  private set { SetField(ref _currentTc, value, nameof(CurrentTc)); } }

        private TDeckState _deckState;
        [XmlIgnore]
        public TDeckState DeckState { get { return _deckState; } private set { SetField(ref _deckState, value, nameof(DeckState)); } }

        private TDeckControl _deckControl;
        [XmlIgnore]
        public TDeckControl DeckControl { get { return _deckControl; }  private set { SetField(ref _deckControl, value, nameof(DeckControl)); } }

        public IEnumerable<IPlayoutServerChannel> Channels { get { return ownerServer.Channels; } }

        public void Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName)
        {
            _tcFormat = channel.VideoFormat;
            _recorder?.Capture(channel.Id, tcIn.ToSMPTETimecodeString(channel.VideoFormat), tcOut.ToSMPTETimecodeString(channel.VideoFormat), fileName);
        }
        public void Abort()
        {
            _recorder?.Abort();
        }

        public void Play()
        {
            _recorder?.Play();
        }
        public void Stop()
        {
            _recorder?.Stop();
        }

        public void FastForward()
        {
            _recorder.FastForward();
        }

        public void Rewind()
        {
            _recorder.Rewind();
        }

        
    }
}
