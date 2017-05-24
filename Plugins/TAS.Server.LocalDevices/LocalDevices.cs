using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.ComponentModel.Composition;
using System.Xml;
using System.Collections.Specialized;
using TAS.Server.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class LocalDevices : IEnginePluginFactory
    {

        NLog.Logger Logger = NLog.LogManager.GetLogger("TAS.Server.LocalDevices");

        [ImportingConstructor]
        public LocalDevices([Import("AppSettings")] NameValueCollection settings)
        {
            DeserializeElements(Path.Combine(Directory.GetCurrentDirectory(), settings["LocalDevices"]));
            Initialize();
        }

        public IEnumerable<Type> Types()
        {
            return new[] { typeof(LocalGpiDeviceBinding) };
        }
        
        public object CreateEnginePlugin(IEngine engine, Type type)
        {
            if (type.IsAssignableFrom(typeof(LocalGpiDeviceBinding)))
                return EngineBindings.FirstOrDefault(b => b.IdEngine == (engine as IPersistent).Id);
            return null;
        }

        public void DeserializeElements(string settingsFileName)
        {
            Debug.WriteLine($"Deserializing LocalDevices from {settingsFileName}");
            Logger.Debug($"Deserializing LocalDevices from {settingsFileName}");
            try
            {
                if (!string.IsNullOrEmpty(settingsFileName) && File.Exists(settingsFileName))
                {
                    XmlDocument settings = new XmlDocument();
                    settings.Load(settingsFileName);
                    var devicesXml = settings.DocumentElement.SelectSingleNode("Devices");
                    XmlSerializer deviceSerializer = new XmlSerializer(typeof(AdvantechDevice));
                    foreach (XmlNode deviceXml in devicesXml.SelectNodes("AdvantechDevice"))
                        Devices.Add((AdvantechDevice)deviceSerializer.Deserialize(new StringReader(deviceXml.OuterXml)));
                    var engineBindingsXml = settings.DocumentElement.SelectSingleNode("EngineBindings");
                    XmlSerializer bindingSerializer = new XmlSerializer(typeof(LocalGpiDeviceBinding), new XmlRootAttribute("EngineBinding"));
                    foreach (XmlNode bindingXml in engineBindingsXml.SelectNodes("EngineBinding"))
                        EngineBindings.Add((LocalGpiDeviceBinding)bindingSerializer.Deserialize(new StringReader(bindingXml.OuterXml)));
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                Logger.Error(e, $"Exception while DeserializeElements:\n {e}");
            }
        }

        public List<AdvantechDevice> Devices = new List<AdvantechDevice>();
        public List<LocalGpiDeviceBinding> EngineBindings = new List<LocalGpiDeviceBinding>();

        public void Initialize()
        {
            if (Devices.Count > 0)
            {
                foreach (AdvantechDevice device in Devices)
                {
                    Debug.WriteLine($"Initializing AdvantechDevice {device.DeviceId}");
                    Logger.Debug($"Initializing AdvantechDevice {device.DeviceId}");
                    device.Initialize();
                }
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
            Debug.WriteLine("Startting AdvantechPoolingThread thread");
            Logger.Debug("Startting AdvantechPoolingThread thread");
            while (!disposed)
            {
                try
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
                                        foreach (LocalGpiDeviceBinding binding in EngineBindings)
                                            binding.NotifyChange(device.DeviceId, port, bit, (newPortState & 0x1) > 0);
                                    }
                                    changedBits = changedBits >> 1;
                                    newPortState = (byte)(newPortState >> 1);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Logger.Warn(e, $"Exception on AdvantechPoolingThread:\n{e}");
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
