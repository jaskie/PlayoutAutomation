using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class Recorder : ProxyBase, IRecorder
    {
        public int Id
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

        public bool Play()
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            throw new NotImplementedException();
        }
    }
}
