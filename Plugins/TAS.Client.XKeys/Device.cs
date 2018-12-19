using System;
using System.Collections.Generic;
using NLog;
using PIEHid64Net;

namespace TAS.Client.XKeys
{
    internal class Device: PIEDataHandler, PIEErrorHandler, IDisposable
    {
        private readonly DeviceEnumerator _ownerEnumerator;
        private readonly byte[] _oldData;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Device(DeviceEnumerator ownerEnumerator, PIEDevice pieDevice)
        {
            _ownerEnumerator = ownerEnumerator;
            PieDevice = pieDevice;
            pieDevice.SetupInterface();
            if (!string.Equals(pieDevice.ProductString, "XK-24 HID", StringComparison.Ordinal))
            {
                Logger.Error("This plugin does not support the {0} device", pieDevice.ProductString);
                return;
            }
            pieDevice.SetDataCallback(this);
            pieDevice.SetErrorCallback(this);
            _oldData = new byte[pieDevice.ReadLength];
            GreenIndicatorLight(pieDevice);
            Dim(pieDevice);
        }

        public PIEDevice PieDevice { get; }

        public void HandlePIEHidData(byte[] data, PIEDevice sourceDevice, int error)
        {
            try
            {
                lock (_oldData)
                {
                    CheckKeys(data[1], 0, data[3], _oldData[3], data);
                    CheckKeys(data[1], 1, data[4], _oldData[4], data);
                    CheckKeys(data[1], 2, data[5], _oldData[5], data);
                    CheckKeys(data[1], 3, data[6], _oldData[6], data);
                    Buffer.BlockCopy(data, 0, _oldData, 0, (int) PieDevice.ReadLength);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void HandlePIEHidError(PIEDevice sourceDevice, long error)
        {
            Logger.Debug("Error received from {0}, error code: {1}", sourceDevice, error);
        }
        
        private void CheckKeys(byte unitId, int column, byte newValues, byte oldValues, byte[] alldata)
        {
            var changedBits = newValues ^ oldValues;
            for (byte bit = 0; bit < 8; bit++)
            {
                if ((changedBits & 0x1) > 0)
                    _ownerEnumerator.KeyNotify(unitId, column * 8 + bit, (newValues & 0x1) > 0, GetAllKeys(alldata));
                changedBits = changedBits >> 1;
                newValues = (byte) (newValues >> 1);
            }
        }

        private static List<int> GetAllKeys(byte[] allData)
        {
            var keys = new List<int>();
            for (var columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                for (var bit = 0; bit < 8; bit++)
                {
                    var columnData = allData[columnIndex + 3];
                    if (((1 << bit) & columnData) != 0)
                        keys.Add(columnIndex * 8 + bit);
                }
            }
            return keys;
        }

        private static void GreenIndicatorLight(PIEDevice device)
        {
            if (device.WriteLength < 4)
                return;
            var wData = new byte[device.WriteLength];
            wData[1] = 179; //0xb3
            wData[2] = 6;   //6 for green, 7 for red
            wData[3] = 1;   //0=off, 1=on, 2=flash
            var result = 404;
            while (result == 404)
                result = device.WriteData(wData);
        }

        private static void Dim(PIEDevice device)
        {
            if (device.WriteLength < 4)
                return;
            var wData = new byte[device.WriteLength];
            wData[1] = 182;
            wData[2] = 0; // bank 1
            var result = 404;
            while (result == 404)
                result = device.WriteData(wData);
            wData[2] = 1; // bank 2
            result = 404;
            while (result == 404)
                result = device.WriteData(wData);
        }

        public void Dispose()
        {
            PieDevice.CloseInterface();
        }
    }
}
