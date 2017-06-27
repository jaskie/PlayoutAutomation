using System;
using System.Threading;
using Automation.BDaq;
using System.Xml.Serialization;


namespace TAS.Server
{
    public class AdvantechDevice : IDisposable
    {
        private DeviceInformation _deviceInformation;
        private InstantDiCtrl _di;
        private InstantDoCtrl _do;
        private int _disposed;
        private readonly object _writeLock = new object();

        [XmlIgnore]
        public byte[] InputPortState;

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
            InputPortState = new byte[InputPortCount];
            _do = new InstantDoCtrl();
            _do.SelectedDevice = _deviceInformation;
            OutputPortCount = _do.Features.PortCount;
        }

        public bool Read(int port, out byte currentData, out byte oldData)
        {
            oldData = InputPortState[port];
            bool ret = _di.Read(port, out currentData) == ErrorCode.Success;
            InputPortState[port] = currentData;
            return ret;
        }

        public bool Write(int port, int pin, bool value)
        {
            lock (_writeLock)
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

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
            {
                _di?.Dispose();
                _do?.Dispose();
            }
        }
    }
}

