using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using PIEHid64Net;

namespace TAS.Client.XKeys
{
    public class DeviceEnumerator: IDisposable, IDeviceEnumerator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<Device> _devices = new List<Device>();
        private int _disposedValue;

        public DeviceEnumerator()
        {
            var enumerationThread = new Thread(EnumerationThreadProc)
            {
                IsBackground = true,
                Name = "XKeys device enumeration thread",
                Priority = ThreadPriority.BelowNormal
            };
            enumerationThread.Start();
        }

        internal void KeyNotify(byte unitId, int keyNr, bool pressed, IReadOnlyList<int> allKeys)
        {
            KeyNotified?.Invoke(this, new KeyNotifyEventArgs(unitId, keyNr, pressed, allKeys));
        }
        
        public event EventHandler<KeyNotifyEventArgs> KeyNotified;

        private void EnumerationThreadProc()
        {
            var oldDevices = new PIEDevice[0];
            while (_disposedValue == 0)
            {
                try
                {
                    var devices = PIEDevice.EnumeratePIE();
                    foreach (var pieDevice in devices.Where(d => d.HidUsagePage == 0xC && !oldDevices.Any(od => DeviceEquals(od, d))))
                    {
                        var device = new Device(this, pieDevice);
                        lock (((IList)_devices).SyncRoot)
                            _devices.Add(device);
                        DeviceConnected?.Invoke(this, new DeviceEventArgs(device));
                        Logger.Info("New device connected {0}:{1}", pieDevice.Pid, pieDevice.Vid);
                    }
                    foreach (var pieDevice in oldDevices.Where(d => d.HidUsagePage == 0xC && !devices.Any(od => DeviceEquals(od, d))))
                    {
                        var device = _devices.FirstOrDefault(d => DeviceEquals(d.PieDevice, pieDevice));
                        if (device == null)
                            continue;
                        device.Dispose();
                        lock (((IList)_devices).SyncRoot)
                            _devices.Remove(device);
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

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposedValue, 1);
            _devices.ForEach(d => d.Dispose());
            _devices.Clear();
        }

        public void SetBacklight(byte unitId, int keyNr, BacklightColorEnum color, bool blinking)
        {
            lock (((IList) _devices).SyncRoot)
            {
                _devices.ForEach(d =>
                {
                    if (d.UnitId == unitId)
                        d.SetBackLight(keyNr, color, blinking);
                });
            }
        }

        public event EventHandler<DeviceEventArgs> DeviceConnected;
    }
}
