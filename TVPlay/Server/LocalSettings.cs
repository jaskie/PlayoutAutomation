using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Automation.BDaq;
using System.Xml.Serialization;
using System.Threading;
using TAS.Common;

namespace TAS.Server
{
    public class LocalSettings: IDisposable
    {
        [XmlArray]
        public AdvantechDevice[] AdvantechDevices = new AdvantechDevice[0];

        [XmlArray]
        public EngineSettings[] Engines = new EngineSettings[0];
        public void Initialize()
        {
            if (AdvantechDevices.Length > 0)
            {
                foreach (AdvantechDevice device in AdvantechDevices)
                    device.Initialize();
                Thread poolingThread = new Thread(_advantechPoolingThreadExecute);
                poolingThread.IsBackground = true;
                poolingThread.Name = string.Format("Thread for Advantech devices pooling");
                poolingThread.Priority = ThreadPriority.AboveNormal;
                poolingThread.Start();
            }
            foreach (EngineSettings engine in Engines)
                engine.Owner = this;
        }
        
        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                foreach (AdvantechDevice device in AdvantechDevices)
                    device.Dispose();
            }
        }

        void _advantechPoolingThreadExecute()
        {
            byte newPortState, oldPortState;
            while (!disposed)
            {
                foreach (AdvantechDevice device in AdvantechDevices)
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
                                        settings.NotifyChange(device.DeviceId, port, bit, (newPortState & 0x1) > 0);
                                }
                                changedBits = changedBits >> 1;
                                newPortState = (byte)(newPortState >> 1);
                            }
                        }
                    }
                }
                Thread.Sleep(5);
            }
        }

        public bool SetPortState(byte deviceId, int port, byte pin, bool value)
        {
            AdvantechDevice device = AdvantechDevices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
                return device.Write(port, pin, value);
            return false;
        }

        public class AdvantechDevice:IDisposable
        {
            [XmlAttribute]
            public byte DeviceId;
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
                _deviceInformation = new DeviceInformation(DeviceId);
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

    public class GPIPin
    {
        [XmlAttribute]
        public int Param;
        [XmlAttribute]
        public byte DeviceId;
        [XmlAttribute]
        public int PortNumber;
        [XmlAttribute]
        public byte PinNumber;
    }

    public class EngineSettings
    {
        internal LocalSettings Owner;
        public event Action StartPressed;
        
        [XmlAttribute]
        public UInt64 IdEngine;
        public GPIPin Start;
        public GPIPin[] Logos;
        public GPIPin[] Crawls;
        public GPIPin[] Parentals;

        void _actionCheckAndExecute(Action action, GPIPin pin, byte deviceId, byte port, byte bit)
        {
            if (action !=null &&  pin != null && deviceId == pin.DeviceId && port == pin.PortNumber && bit == pin.PinNumber)
                    action();
        }

        internal void NotifyChange(byte deviceId, byte port, byte bit, bool newValue)
        {
            var pin = Start;
            if (newValue)
                _actionCheckAndExecute(StartPressed, Start, deviceId, port, bit);
        }
        
        TParental _parental;
        [XmlIgnore]
        public TParental Parental
        {
            get { return _parental; }
            set
            {
                if (_parental != value)
                {
                    _parental = value;
                    _setSinglePin(Parentals, (int)value);
                }
            }
        }

        TLogo _logo;
        [XmlIgnore]
        public TLogo Logo
        {
            get { return _logo; }
            set
            {
                if (_logo != value)
                {
                    _logo = value;
                    _setSinglePin(Logos, (int)value);
                }
            }
        }

        TCrawl _crawl;
        [XmlIgnore]
        public TCrawl Crawl
        {
            get { return _crawl; }
            set
            {
                if (_crawl != value)
                {
                    _crawl = value;
                    _setSinglePin(Crawls, (int)value);
                }
            }
        }
        
        void _setSinglePin(GPIPin[] pins, int value)
        {
            var owner = Owner;
            if (pins != null && owner != null)
            {
                pins.Where(p => p.Param != value).ToList().ForEach(p => owner.SetPortState(p.DeviceId, p.PortNumber, p.PinNumber, false));
                pins.Where(p => p.Param == value).ToList().ForEach(p => owner.SetPortState(p.DeviceId, p.PortNumber, p.PinNumber, true));
            }
        }

    }
}
