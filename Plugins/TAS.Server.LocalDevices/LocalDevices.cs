using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.ComponentModel.Composition;
using System.Xml;
using System.Collections.Specialized;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class LocalDevices : IEnginePluginFactory
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        [ImportingConstructor]
        public LocalDevices([Import("AppSettings")] NameValueCollection settings)
        {
            DeserializeElements(Path.Combine(Directory.GetCurrentDirectory(), settings["LocalDevices"] ?? "Configuration\\LocalDevices.xml"));
            Initialize();
        }

        public Type Type { get; } = typeof(LocalGpiDeviceBinding);

        public object CreateEnginePlugin(IEngine engine)
        {
            return EngineBindings.FirstOrDefault(b => b.IdEngine == (engine as IPersistent)?.Id);
        }

        public void DeserializeElements(string settingsFileName)
        {
            Debug.WriteLine($"Deserializing LocalDevices from {settingsFileName}");
            Logger.Debug($"Deserializing LocalDevices from {settingsFileName}");
            try
            {
                if (string.IsNullOrEmpty(settingsFileName) || !File.Exists(settingsFileName))
                    return;
                var settings = new XmlDocument();
                settings.Load(settingsFileName);
                if (settings.DocumentElement == null)
                    return;
                var devicesXml = settings.DocumentElement.SelectSingleNode("Devices");
                var advantechDeviceNodes = devicesXml?.SelectNodes("AdvantechDevice");
                if (advantechDeviceNodes == null)
                    return;
                var deviceSerializer = new XmlSerializer(typeof(AdvantechDevice));
                foreach (XmlNode deviceXml in advantechDeviceNodes)
                    Devices.Add(
                        (AdvantechDevice) deviceSerializer
                            .Deserialize(new StringReader(deviceXml.OuterXml)));
                var engineBindingsXml = settings.DocumentElement.SelectSingleNode("EngineBindings");
                var engineBindingsXmlNodes = engineBindingsXml?.SelectNodes("EngineBinding");
                if (engineBindingsXmlNodes == null)
                    return;
                var bindingSerializer = new XmlSerializer(typeof(LocalGpiDeviceBinding),
                    new XmlRootAttribute("EngineBinding"));
                foreach (XmlNode bindingXml in engineBindingsXmlNodes)
                    EngineBindings.Add(
                        (LocalGpiDeviceBinding) bindingSerializer.Deserialize(new StringReader(
                            bindingXml
                                .OuterXml)));
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
                foreach (var device in Devices)
                {
                    Debug.WriteLine($"Initializing AdvantechDevice {device.DeviceId}");
                    Logger.Debug($"Initializing AdvantechDevice {device.DeviceId}");
                    device.Initialize();
                }
                var poolingThread = new Thread(_advantechPoolingThreadExecute)
                {
                    IsBackground = true,
                    Name = "Thread for Advantech devices pooling",
                    Priority = ThreadPriority.AboveNormal
                };
                poolingThread.Start();
            }
            foreach (var binding in EngineBindings)
                binding.Owner = this;
        }

        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            foreach (var device in Devices)
                device.Dispose();
        }

        private void _advantechPoolingThreadExecute()
        {
            Debug.WriteLine("Startting AdvantechPoolingThread thread");
            Logger.Debug("Startting AdvantechPoolingThread thread");
            while (_disposed == default(int))
            {
                try
                {
                    foreach (var device in Devices)
                    {
                        for (byte port = 0; port < device.InputPortCount; port++)
                        {
                            if (!device.Read(port, out var newPortState, out var oldPortState))
                                continue;
                            var changedBits = newPortState ^ oldPortState;
                            for (byte bit = 0; bit < 8; bit++)
                            {
                                if ((changedBits & 0x1) > 0)
                                {
                                    foreach (var binding in EngineBindings)
                                        binding.NotifyChange(device.DeviceId, port, bit, (newPortState & 0x1) > 0);
                                }
                                changedBits = changedBits >> 1;
                                newPortState = (byte)(newPortState >> 1);
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
            var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
            return device != null && device.Write(port, pin, value);
        }

    }
}
