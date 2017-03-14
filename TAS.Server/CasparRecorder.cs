using Svt.Caspar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server
{

    public class CasparRecorder: DtoBase, IRecorder
    {
        internal CasparServer ownerServer;
        private Recorder _recorder;
        internal void SetRecorder(Recorder value)
        {
            if (_recorder != value)
                _recorder = value;
        }

        public int RecorderNumber { get; set; }
        public int Id { get; set; }
        public string RecorderName { get; set; }
        public void Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName)
        {
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
