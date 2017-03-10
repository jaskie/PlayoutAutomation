using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server
{

    public class CasparRecorder: DtoBase, IRecorder
    {
        public int RecorderNumber { get; set; }
        public int RecordChannelNumber { get; set; }
        public string RecorderName { get; set; }
        public void Capture(string fileName, TimeSpan tcIn, TimeSpan tcOut)
        {

        }
        public void AbortCapture()
        {

        }

        public void Play()
        {

        }
        public void Stop()
        {

        }
    }
}
