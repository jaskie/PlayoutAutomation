using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using PIEHid64Net;

namespace TAS.Client.XKeys
{
    public static class XKeysDeviceEnumerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly List<XKeysDevice> Devices = new List<XKeysDevice>();

        static XKeysDeviceEnumerator()
        {
            var enumerationThread = new Thread(EnumerationThreadProc)
            {
                IsBackground = true,
                Name = "XKeys device enumeration thread",
                Priority = ThreadPriority.BelowNormal
            };
            enumerationThread.Start();
        }

        internal static void KeyNotify(XKeysDevice device, int keyNr, bool pressed, IReadOnlyList<int> allKeys)
        {
            KeyNotified?.Invoke(null, new KeyNotifyEventArgs(device, keyNr, pressed, allKeys));
        }
        
        public static event EventHandler<KeyNotifyEventArgs> KeyNotified;

        private static void EnumerationThreadProc()
        {
            var oldDevices = new PIEDevice[0];
            while (true)
            {
                try
                {
                    var devices = PIEDevice.EnumeratePIE();
                    foreach (var pieDevice in devices.Where(d => d.HidUsagePage == 0xC && !oldDevices.Any(od => DeviceEquals(od, d))))
                    {
                        var device = new XKeysDevice(pieDevice);
                        lock (((IList)Devices).SyncRoot)
                            Devices.Add(device);
                        DeviceConnected?.Invoke(null, device);
                        Logger.Info("New device connected {0}:{1}", pieDevice.Pid, pieDevice.Vid);
                    }
                    foreach (var pieDevice in oldDevices.Where(d => d.HidUsagePage == 0xC && !devices.Any(od => DeviceEquals(od, d))))
                    {
                        var device = Devices.FirstOrDefault(d => DeviceEquals(d.PieDevice, pieDevice));
                        if (device == null)
                            continue;
                        device.Dispose();
                        lock (((IList)Devices).SyncRoot)
                            Devices.Remove(device);
                        Logger.Info("Device disconnected {0}:{1}", pieDevice.Pid, pieDevice.Vid);
                    }
                    oldDevices = devices;
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private static bool DeviceEquals(PIEDevice first, PIEDevice second)
        {
            return string.Equals(first.Path, second.Path, StringComparison.Ordinal);
        }


        public static void SetBacklight(byte unitId, int keyNr, BacklightColorEnum color, bool blinking)
        {
            lock (((IList) Devices).SyncRoot)
            {
                Devices.ForEach(d =>
                {
                    if (d.UnitId == unitId)
                        d.SetBackLight(keyNr, color, blinking);
                });
            }
        }

        public static event EventHandler<XKeysDevice> DeviceConnected;
    }
}
