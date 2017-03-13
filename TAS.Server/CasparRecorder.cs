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
        internal CasparServer ownerServer;
        public int RecorderNumber { get; set; }
        public int Id { get; set; }
        public string RecorderName { get; set; }
        public void Capture(string fileName, TimeSpan tcIn, TimeSpan tcOut)
        {

        }
        public bool AbortCapture()
        {
            return true;
        }

        public bool Play()
        {

            return true;
        }
        public bool Stop()
        {
            return true;
        }
    }
}
