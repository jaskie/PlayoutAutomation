using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class Recorder : ProxyBase, IRecorder
    {
        public IEnumerable<IPlayoutServerChannel> Channels
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan CurrentTc
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TDeckControl DeckControl
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TDeckState DeckState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string RecorderName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMediaDirectory RecordingDirectory
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMedia RecordingMedia
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName)
        {
            throw new NotImplementedException();
        }

        public void FastForward()
        {
            throw new NotImplementedException();
        }

        public void GoToTimecode(TimeSpan tc, TVideoFormat format)
        {
            throw new NotImplementedException();
        }

        public bool Play()
        {
            throw new NotImplementedException();
        }

        public void Rewind()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }

        void IRecorder.Play()
        {
            throw new NotImplementedException();
        }
    }
}
