using NAudio.Wave;

namespace TAS.Client.NDIVideoPreview.Model
{
    public class AudioDevice
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }

        public static AudioDevice[] EnumerateDevices()
        {
            int waveOutDeviceCount = WaveOut.DeviceCount;
            var result = new AudioDevice[waveOutDeviceCount];
            for (int waveOutDevice = 0; waveOutDevice < waveOutDeviceCount; waveOutDevice++)
            {
                var deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                result[waveOutDevice] = new AudioDevice { Id = waveOutDevice, DeviceName = deviceInfo.ProductName };
            }
            return result;
        }
    }

}
