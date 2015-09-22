using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class LocalDevices : IDisposable, IInitializable
    {
        [XmlArray]
        public AdvantechDevice[] Devices = new AdvantechDevice[0];

        [XmlArray]
        [XmlArrayItem("Binding")]
        public LocalGpiDeviceBinding[] EngineBindings = new LocalGpiDeviceBinding[0];
        public void Initialize()
        {
            if (Devices.Length > 0)
            {
                foreach (AdvantechDevice device in Devices)
                    device.Initialize();
                Thread poolingThread = new Thread(_advantechPoolingThreadExecute);
                poolingThread.IsBackground = true;
                poolingThread.Name = string.Format("Thread for Advantech devices pooling");
                poolingThread.Priority = ThreadPriority.AboveNormal;
                poolingThread.Start();
            }
            foreach (LocalGpiDeviceBinding binding in EngineBindings)
                binding.Owner = this;
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                foreach (AdvantechDevice device in Devices)
                    device.Dispose();
            }
        }

        void _advantechPoolingThreadExecute()
        {
            byte newPortState, oldPortState;
            while (!disposed)
            {
                foreach (AdvantechDevice device in Devices)
                {
                    for (byte port = 0; port < device.InputPortCount; port++)
                    {
                        if (device.Read(port, out newPortState, out oldPortState))
                        {
                            int changedBits = newPortState ^ oldPortState;
                            for (byte bit = 0; bit < 8; bit++)
                            {
                                if ((changedBits & 0x1) > 0)
                                {
                                    foreach (LocalGpiDeviceBinding settings in EngineBindings)
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
            AdvantechDevice device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            if (device != null)
                return device.Write(port, pin, value);
            return false;
        }


    }

  

}
