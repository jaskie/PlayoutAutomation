using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automation.BDaq;
using System.Xml.Serialization;
using System.Threading;

namespace TAS.Server
{
    public class LocalSettings: IDisposable
    {
        [XmlArrayItem("Advantech")]
        public AdvantechDevice[] GPIDevices = new AdvantechDevice[0];

        [XmlArrayItem("Engine")]
        public EngineSettings[] Engines = new EngineSettings[0];
        public void Initialize()
        {
            if (GPIDevices.Length > 0)
            {
                foreach (AdvantechDevice device in GPIDevices)
                    device.Initialize();
                Thread poolingThread = new Thread(_advantechPoolingThreadExecute);
                poolingThread.IsBackground = true;
                poolingThread.Name = string.Format("Thread for Advantech devices pooling");
                poolingThread.Priority = ThreadPriority.AboveNormal;
                poolingThread.Start();
            }
        }
        
        //public event EventHandler<LocalGPIChangedEventArgs> LocalGPIChanged;

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                foreach (AdvantechDevice device in GPIDevices)
                    device.Dispose();
            }
        }

        void _advantechPoolingThreadExecute()
        {
            byte newPortState, oldPortState;
            while (!disposed)
            {
                foreach (AdvantechDevice device in GPIDevices)
                {
                    for (byte port = 0; port < device.InputPortCount; port++)
                    {
                        ErrorCode err = device.Read(port, out newPortState, out oldPortState);
                        if (err == ErrorCode.Success)
                        {
                            int changedBits = newPortState ^ oldPortState;
                            for (byte bit = 0; bit < 8; bit++)
                            {
                                if ((changedBits & 0x1) > 0)
                                {
                                    foreach (EngineSettings settings in Engines)
                                    {
                                        if (settings.Start.DeviceID == device.Id
                                            && settings.Start.PortNumber == port
                                            && settings.Start.PinNumber == bit
                                            && (newPortState & 0x1) > 0)
                                            settings.NotifyStarted(this);
                                    }
                                    //var h = LocalGPIChanged;
                                    //if (h != null)
                                    //{
                                    //    h(this, new LocalGPIChangedEventArgs(device.Id, port, bit, (newPortState & 0x1) > 0));
                                    //}
                                    changedBits = changedBits >> 1;
                                    newPortState = (byte)(newPortState >> 1);
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(5);
            }
        }


        public class AdvantechDevice:IDisposable
        {
            public byte Id;
            DeviceInformation _deviceInformation;
            InstantDiCtrl _di;
            InstantDoCtrl _do;
            
            [XmlIgnore]
            public int InputPortCount;
            [XmlIgnore]
            public int OutputPortCount;

            [XmlIgnore]
            public byte[] inputPortState;
                        
            public void Initialize()
            {
                _deviceInformation = new DeviceInformation(Id);
                _di = new InstantDiCtrl();
                _di.SelectedDevice = _deviceInformation;
                InputPortCount = _di.Features.PortCount;
                inputPortState = new byte[InputPortCount];
                _do = new InstantDoCtrl();
                _do.SelectedDevice = _deviceInformation;
                OutputPortCount = _do.Features.PortCount;
            }

            public ErrorCode Read(int port, out byte currentData, out byte oldData)
            {
                oldData = inputPortState[port];
                ErrorCode ret = _di.Read(port, out currentData);
                inputPortState[port] = currentData;
                return ret;
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

    //public class LocalGPIChangedEventArgs: EventArgs
    //{
    //    public byte DeviceID { get; private set; }
    //    public byte PortNumber { get; private set; }
    //    public byte PinNumber { get; private set; }
    //    public bool IsSet { get; private set; }
    //    public LocalGPIChangedEventArgs(byte deviceId, byte portNumber, byte pinNumber, bool isSet)
    //    {
    //        DeviceID = deviceId;
    //        PortNumber = portNumber;
    //        PinNumber = pinNumber;
    //        IsSet = isSet;
    //    }
    //}

    public struct GPIPin
    {
        public byte DeviceID;
        public byte PortNumber;
        public byte PinNumber;
    }

    public struct EngineSettings
    {
        public UInt64 EngineID;
        public GPIPin Start;
        public event EventHandler<EventArgs> Started;
        internal void NotifyStarted(object sender)
        {
            var h = Started;
            if (h != null)
                h(sender, new EventArgs());
        }
    }
}
