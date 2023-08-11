using System;
using System.Threading;
using System.Windows;
using Automation.BDaq;

namespace TAS.Server.Advantech.Model
{
    public class AdvantechDevice : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private DeviceInformation _deviceInformation;
        private InstantDiCtrl _di;
        private InstantDoCtrl _do;
        private int _disposed;
        private readonly object _writeLock = new object();
        
        public byte[] InputPortState;        

        internal int InputPortCount;
        internal int OutputPortCount;

        public AdvantechDevice(byte deviceId)
        {
            try
            {
                _deviceInformation = new DeviceInformation(deviceId);
                _di = new InstantDiCtrl { SelectedDevice = _deviceInformation };
                InputPortCount = _di.Features.PortCount;
                InputPortState = new byte[InputPortCount];
                _do = new InstantDoCtrl { SelectedDevice = _deviceInformation };
                OutputPortCount = _do.Features.PortCount;
            }
            catch (Exception e)
            {               
                Logger.Error(e);
            }
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
                if (_do.Read(port, out var portValue) != ErrorCode.Success)
                    return false;
                if (value)
                    portValue = (byte)(portValue | 0x1 << pin);
                else
                    portValue = (byte)(portValue & ~(0x1 << pin));
                return _do.Write(port, portValue) == ErrorCode.Success;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            _di?.Dispose();
            _do?.Dispose();
            Logger.Trace("Advantech device disposed {0}", _deviceInformation.DeviceNumber);
        }
    }
}

