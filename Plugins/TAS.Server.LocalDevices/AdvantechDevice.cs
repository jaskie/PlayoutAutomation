using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automation.BDaq;
using System.Xml.Serialization;


namespace TAS.Server
{
    public class AdvantechDevice : IDisposable
    {
        DeviceInformation _deviceInformation;
        InstantDiCtrl _di;
        InstantDoCtrl _do;

        [NonSerialized]
        public byte[] inputPortState;

        [XmlAttribute]
        public byte DeviceId;

        internal int InputPortCount;
        internal int OutputPortCount;

        public void Initialize()
        {
            _deviceInformation = new DeviceInformation(DeviceId);
            _di = new InstantDiCtrl();
            _di.SelectedDevice = _deviceInformation;
            InputPortCount = _di.Features.PortCount;
            inputPortState = new byte[InputPortCount];
            _do = new InstantDoCtrl();
            _do.SelectedDevice = _deviceInformation;
            OutputPortCount = _do.Features.PortCount;
        }

        public bool Read(int port, out byte currentData, out byte oldData)
        {
            oldData = inputPortState[port];
            bool ret = _di.Read(port, out currentData) == ErrorCode.Success;
            inputPortState[port] = currentData;
            return ret;
        }

        object writeLock = new object();
        public bool Write(int port, int pin, bool value)
        {
            lock (writeLock)
            {
                byte portValue;
                if (_do.Read(port, out portValue) == ErrorCode.Success)
                {
                    if (value)
                        portValue = (byte)(portValue | 0x1 << pin);
                    else
                        portValue = (byte)(portValue & ~(0x1 << pin));
                    return _do.Write(portValue, portValue) == ErrorCode.Success;
                }
            }
            return false;
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                if (_di != null)
                    _di.Dispose();
                if (_do != null)
                    _do.Dispose();
            }
        }
    }
}

