using System;
using System.Collections.Generic;
using NLog;
using PIEHid64Net;

namespace TAS.Client.XKeys
{
    public enum DeviceModelEnum
    {
        Unsupported,
        Xk24,
        Xk12JogAndShuttle
    }

    public class Device: PIEDataHandler, PIEErrorHandler, IDisposable
    {
        private readonly byte[] _oldData;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Device(PIEDevice pieDevice)
        {
            PieDevice = pieDevice;
            DeviceModel = GetDeviceModel(pieDevice);
            if (DeviceModel == DeviceModelEnum.Unsupported)
            {
                Logger.Error("This plugin does not support the {0} device in mode {1}", pieDevice.ProductString, pieDevice.Pid);
                return;
            }
            pieDevice.SetupInterface();
            pieDevice.SetDataCallback(this);
            pieDevice.SetErrorCallback(this);
            _oldData = new byte[pieDevice.ReadLength];
            GreenIndicatorLight(pieDevice);
            Dim();
            UnitId = ReadUnitId();
        }

        public PIEDevice PieDevice { get; }

        public DeviceModelEnum DeviceModel { get; }

        public byte UnitId { get; }

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

        public void Dispose()
        {
            PieDevice.CloseInterface();
        }

        private void CheckKeys(byte unitId, int column, byte newValues, byte oldValues, byte[] alldata)
        {
            var changedBits = newValues ^ oldValues;
            for (byte bit = 0; bit < 8; bit++)
            {
                if ((changedBits & 0x1) > 0)
                    DeviceEnumerator.KeyNotify(unitId, column * 8 + bit, (newValues & 0x1) > 0, GetAllKeys(alldata));
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

        private void Dim()
        {
            if (PieDevice.WriteLength < 4)
                return;
            var wData = new byte[PieDevice.WriteLength];
            wData[1] = 182;
            wData[2] = 0; // bank 1
            var result = 404;
            while (result == 404)
                result = PieDevice.WriteData(wData);
            wData[2] = 1; // bank 2
            result = 404;
            while (result == 404)
                result = PieDevice.WriteData(wData);
        }
        

        public void SetBackLight(int keyNr, BacklightColorEnum color, bool blinking)
        {
            var wData = new byte[PieDevice.WriteLength];
            wData[1] = 181; //b5
            var result = 404;
            switch (color)
            {
                case BacklightColorEnum.None:
                    wData[2] = (byte) keyNr;
                    wData[3] = 0;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    result = 404;
                    wData[2] = (byte) (keyNr + 32);
                    wData[3] = 0;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    break;
                case BacklightColorEnum.Red:
                    wData[2] = (byte) keyNr;
                    wData[3] = 0;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    result = 404;
                    wData[2] = (byte) (keyNr + 32);
                    wData[3] = blinking ? (byte) 2 : (byte) 1;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    break;
                case BacklightColorEnum.Blue:
                    wData[2] = (byte) keyNr;
                    wData[3] = blinking ? (byte) 2 : (byte) 1;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    result = 404;
                    wData[2] = (byte) (keyNr + 32);
                    wData[3] = 0;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    break;
                case BacklightColorEnum.Both:
                    wData[2] = (byte) keyNr;
                    wData[3] = blinking ? (byte) 2 : (byte) 1;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    result = 404;
                    wData[2] = (byte) (keyNr + 32);
                    wData[3] = blinking ? (byte) 2 : (byte) 1;
                    while (result != 0)
                        result = PieDevice.WriteData(wData);
                    break;
            }
        }

        private byte ReadUnitId()
        {
            var wData = new byte[PieDevice.WriteLength];
            wData[1] = 214;
            var result = 404;
            while (result != 0)
                result = PieDevice.WriteData(wData);
            var rData = new byte[PieDevice.ReadLength];
            result = 304;
            while (result != 0)
                result = PieDevice.ReadData(ref rData);
            return rData[1];

        }

        private static DeviceModelEnum GetDeviceModel(PIEDevice device)
        {
            switch (device.Pid)
            {
                case 1029:
                    return DeviceModelEnum.Xk24;
                case 1062:
                    return DeviceModelEnum.Xk12JogAndShuttle;
                default:
                    return DeviceModelEnum.Unsupported;
            }
        }


    }
}
