using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using TAS.Client.Common;
using TAS.Client.NDIVideoPreview.Audio;
using TAS.Client.NDIVideoPreview.Interop;

namespace TAS.Client.NDIVideoPreview
{
    public class VideoPreviewViewmodel : ViewModelBase, Common.Plugin.IVideoPreview
    {
        private const double MinAudioLevel = -60;
        private readonly ObservableCollection<string> _videoSources;
        private string _videoSource;
        private IntPtr _ndiReceiveInstance;
        private Thread _ndiReceiveThread;
        private volatile bool _exitReceiveThread;
        private BitmapSource _videoBitmap;
        private bool _isDisplaySource;
        private bool _displayPopup;
        private bool _isDisplayAudioBars = true;
        private AudioLevelBarViewmodel[] _audioLevels = new AudioLevelBarViewmodel[0];
        private IEnumerable<AudioDevice> _audioDevices;
        private AudioDevice _selectedAudioDevice;

        // NAudio
        private WaveOut _waveOut;
        private WaveFormat _waveFormat;
        private BufferedWaveProvider _bufferedProvider;
        private bool _isPlayAudio;

        // static
        private static Dictionary<string, NDIlib_source_t> _ndiSources;
        private static event EventHandler SourceRefreshed;
        private static readonly IntPtr NdiFindInstance;

        static VideoPreviewViewmodel()
        {
            Ndi.AddRuntimeDir();
            var findDesc = new NDIlib_find_create_t
            {
                p_groups = IntPtr.Zero,
                show_local_sources = true,
                p_extra_ips = IntPtr.Zero
            };
            NdiFindInstance = Ndi.NDIlib_find_create2(ref findDesc);
            var sourcesPoolThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        if (Ndi.NDIlib_find_wait_for_sources(NdiFindInstance, int.MaxValue))
                            RefreshSources();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            })
            {
                Name = "NDI source list pooling thread",
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            sourcesPoolThread.Start();
        }

        private static void RefreshSources()
        {
            var numSources = 0;
            var ndiSources = Ndi.NDIlib_find_get_current_sources(NdiFindInstance, ref numSources);
            if (numSources <= 0)
                return;
            var sourceSizeInBytes = Marshal.SizeOf(typeof(NDIlib_source_t));
            var sources = new Dictionary<string, NDIlib_source_t>();
            for (var i = 0; i < numSources; i++)
            {
                var p = IntPtr.Add(ndiSources, (i * sourceSizeInBytes));
                var src = (NDIlib_source_t)Marshal.PtrToStructure(p, typeof(NDIlib_source_t));
                var ndiName = Ndi.Utf8ToString(src.p_ndi_name);
                sources.Add(ndiName, src);
                Debug.WriteLine($"Added source name:{Ndi.Utf8ToString(src.p_ndi_name)} address :{Ndi.Utf8ToString(src.p_ip_address)}");
            }
            _ndiSources = sources;
            SourceRefreshed?.Invoke(null, EventArgs.Empty);
        }


        public VideoPreviewViewmodel()
        {
            _videoSources = new ObservableCollection<string>(new[] {Common.Properties.Resources._none_});
            _videoSource = _videoSources.FirstOrDefault();
            CommandRefreshSources = new UiCommand(RefreshSources, o => NdiFindInstance != IntPtr.Zero);
            CommandGotoNdiWebsite = new UiCommand(GotoNdiWebsite);
            CommandShowPopup = new UiCommand(o => DisplayPopup = true);
            CommandHidePopup = new UiCommand(o => DisplayPopup = false);
            SourceRefreshed += OnSourceRefreshed;
            RefreshAudioDevices();
            View = new VideoPreviewView { DataContext = this };
        }

        
        public ICommand CommandRefreshSources { get; }
        public ICommand CommandGotoNdiWebsite { get; }
        public ICommand CommandShowPopup { get; }
        public ICommand CommandHidePopup { get; }

        #region IVideoPreview

        public UserControl View { get; }

        /// <summary>
        /// Method accepts address in form ndi://ip_address:port and ndi://ip_address:ndi_name
        /// </summary>
        /// <param name="sourceUrl"></param>
        public void SetSource(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return;
            if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri) && sourceUri.Scheme == "ndi"
                || string.Equals(sourceUrl.Substring(0, sourceUrl.IndexOf(':')), "ndi", StringComparison.InvariantCultureIgnoreCase))
                OnUiThread(() =>
                {
                    if (_ndiSources == null)
                        return;
                    string source = null;
                    if (sourceUri != null)
                        source = _ndiSources.FirstOrDefault(s => Ndi.Utf8ToString(s.Value.p_ip_address) == sourceUri.Host).Key;
                    else
                    {
                        string address = sourceUrl.Substring(sourceUrl.IndexOf("//", StringComparison.Ordinal) + 2);
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            string host = address.Substring(0, address.IndexOf(':'));
                            string name = address.Substring(address.IndexOf(':') + 1);
                            source = _ndiSources.FirstOrDefault(s =>
                               {
                                   string ndiFullAddress = Ndi.Utf8ToString(s.Value.p_ip_address);
                                   string ndiFullName = Ndi.Utf8ToString(s.Value.p_ndi_name);
                                   int openingBraceIndex = ndiFullName.IndexOf('(', 1);
                                   int closingBraceIndex = ndiFullName.IndexOf(')', openingBraceIndex);
                                   if (openingBraceIndex > 0
                                       && closingBraceIndex > openingBraceIndex
                                       && ndiFullAddress.Substring(0, ndiFullAddress.IndexOf(':')).Equals(host, StringComparison.InvariantCultureIgnoreCase)
                                       && ndiFullName.Substring(openingBraceIndex + 1, closingBraceIndex - openingBraceIndex - 1).Equals(name, StringComparison.InvariantCultureIgnoreCase))
                                       return true;
                                   return false;
                               }).Key;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(source))
                        VideoSource = source;
                });
        }

        #endregion IVideoPreview

        public IEnumerable<string> VideoSources => _videoSources;

        public string VideoSource
        {
            get => _videoSource;
            set
            {
                if (SetField(ref _videoSource, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        Disconnect();
                        VideoBitmap = null;
                        AudioLevels = new AudioLevelBarViewmodel[0];
                        IsDisplaySource = _ndiSources.ContainsKey(value);
                        if (IsDisplaySource)
                            Task.Run(() => Connect(value));
                    }
                }
            }
        }

        public bool IsDisplaySource
        {
            get => _isDisplaySource;
            private set
            {
                if (SetField(ref _isDisplaySource, value))
                    NotifyPropertyChanged(nameof(IsDisplayAudioBars));
            }
        }

        public BitmapSource VideoBitmap
        {
            get => _videoBitmap;
            private set => SetField(ref _videoBitmap, value);
        }

        public bool IsDisplayAudioBars
        {
            get => _isDisplayAudioBars && _isDisplaySource;
            set => SetField(ref _isDisplayAudioBars, value);
        }

        public AudioDevice SelectedAudioDevice
        {
            get => _selectedAudioDevice;
            set => SetField(ref _selectedAudioDevice, value);
        }

        public IEnumerable<AudioDevice> AudioDevices
        {
            get => _audioDevices;
            private set => SetField(ref _audioDevices, value);
        }

        public bool IsPlayAudio
        {
            get => _isPlayAudio;
            set => SetField(ref _isPlayAudio, value);
        }

        public AudioLevelBarViewmodel[] AudioLevels
        {
            get => _audioLevels;
            private set => SetField(ref _audioLevels, value);
        }


        protected override void OnDispose()
        {
            Disconnect();
            SourceRefreshed -= OnSourceRefreshed;
        }

        private void GotoNdiWebsite(object obj)
        {
            DisplayPopup = false;
            Process.Start(obj.ToString());
        }

        public bool DisplayPopup
        {
            get => _displayPopup;
            set => SetField(ref _displayPopup, value);
        }

        private async void RefreshSources(object o)
        {
            try
            {
                RefreshAudioDevices();
                await Task.Run(() =>
                {
                    RefreshSources();
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void RefreshAudioDevices()
        {
            AudioDevices = AudioDevice.EnumerateDevices();
            var previousAudioDevice = SelectedAudioDevice;
            SelectedAudioDevice = previousAudioDevice == null
                ? AudioDevices.FirstOrDefault()
                : AudioDevices.FirstOrDefault(d => d.DeviceName.Equals(previousAudioDevice.DeviceName)) ??
                  AudioDevices.FirstOrDefault();
        }


        private void Connect(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName) || _ndiSources == null || !_ndiSources.ContainsKey(sourceName))
                return;
            NDIlib_source_t source = _ndiSources[sourceName];
            NDIlib_recv_create_t recvDescription = new NDIlib_recv_create_t()
            {
                source_to_connect_to = source,
                color_format = NDIlib_recv_color_format_e.NDIlib_recv_color_format_e_BGRX_BGRA,
                bandwidth = NDIlib_recv_bandwidth_e.NDIlib_recv_bandwidth_lowest,
                allow_video_fields = false
            };

            _ndiReceiveInstance = Ndi.NDIlib_recv_create(ref recvDescription);
            if (_ndiReceiveInstance != IntPtr.Zero)
            {
                // start up a thread to receive on
                _ndiReceiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "Newtek Ndi video preview plugin receive thread" };
                _exitReceiveThread = false;
                _ndiReceiveThread.Start();
            }
        }

        private void ReceiveThreadProc()
        {
            var recvInstance = _ndiReceiveInstance;
            if (recvInstance == IntPtr.Zero)
                return;
            var audioDevice = SelectedAudioDevice;
            while (!_exitReceiveThread)
            {
                NDIlib_video_frame_t videoFrame = new NDIlib_video_frame_t();
                NDIlib_audio_frame_t audioFrame = new NDIlib_audio_frame_t();
                NDIlib_metadata_frame_t metadataFrame = new NDIlib_metadata_frame_t();

                switch (Ndi.NDIlib_recv_capture(recvInstance, ref videoFrame, ref audioFrame, ref metadataFrame, 100))
                {
                    case NDIlib_frame_type_e.NDIlib_frame_type_video:
                        if (videoFrame.p_data == IntPtr.Zero)
                        {
                            Ndi.NDIlib_recv_free_video(recvInstance, ref videoFrame);
                            break;
                        }

                        int yres = (int)videoFrame.yres;
                        int xres = (int)videoFrame.xres;

                        double dpiY = 96.0 * (videoFrame.picture_aspect_ratio / ((double)xres / yres));

                        int stride = (int)videoFrame.line_stride_in_bytes;
                        int bufferSize = yres * stride;
                        OnUiThread(() =>
                        {
                            if (VideoBitmap == null
                                || VideoBitmap.PixelWidth != xres
                                || VideoBitmap.PixelHeight != yres)
                                VideoBitmap = new WriteableBitmap(xres, yres, 96, dpiY, System.Windows.Media.PixelFormats.Pbgra32, null);
                            // update the writeable bitmap
                            if ((_videoBitmap is WriteableBitmap videoBitmap) 
                                && videoBitmap.TryLock(TimeSpan.FromSeconds(1)))
                            {
                                videoBitmap.WritePixels(new Int32Rect(0, 0, xres, yres), videoFrame.p_data, bufferSize, stride);
                                videoBitmap.Unlock();
                            }
                            Ndi.NDIlib_recv_free_video(recvInstance, ref videoFrame);
                        });
                        break;
                    case NDIlib_frame_type_e.NDIlib_frame_type_audio:
                        if (!(audioFrame.no_samples == 0 ||
                              audioFrame.p_data == IntPtr.Zero))
                        {
                            // playing audio
                            if (IsPlayAudio && audioDevice != null)
                            {
                                var isFormatChanged = false;
                                if (_waveFormat == null ||
                                    _waveFormat.Channels != audioFrame.no_channels ||
                                    _waveFormat.SampleRate != audioFrame.sample_rate)
                                {
                                    _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioFrame.sample_rate,
                                        audioFrame.no_channels);
                                    isFormatChanged = true;
                                }
                                if (_bufferedProvider == null || isFormatChanged)
                                {
                                    _bufferedProvider = new BufferedWaveProvider(_waveFormat)
                                    {
                                        DiscardOnBufferOverflow = true
                                    };
                                }
                                if (_waveOut == null || isFormatChanged || audioDevice != SelectedAudioDevice)
                                {
                                    _waveOut?.Dispose();
                                    audioDevice = SelectedAudioDevice;
                                    _waveOut = new WaveOut
                                    {
                                        DesiredLatency = 100,
                                        DeviceNumber = audioDevice.Id
                                    };
                                    _waveOut.Init(_bufferedProvider);
                                    _waveOut.Play();
                                }

                                NDIlib_audio_frame_interleaved_32f_t interleavedFrame =
                                    new NDIlib_audio_frame_interleaved_32f_t
                                    {
                                        sample_rate = audioFrame.sample_rate,
                                        no_channels = audioFrame.no_channels,
                                        no_samples = audioFrame.no_samples,
                                        timecode = audioFrame.timecode
                                    };
                                int sizeInBytes = audioFrame.no_samples * audioFrame.no_channels * sizeof(float);
                                byte[] audBuffer = new byte[sizeInBytes];
                                GCHandle handle = GCHandle.Alloc(audBuffer, GCHandleType.Pinned);
                                interleavedFrame.p_data = handle.AddrOfPinnedObject();
                                Ndi.NDIlib_util_audio_to_interleaved_32f(ref audioFrame, ref interleavedFrame);
                                handle.Free();
                                _bufferedProvider.AddSamples(audBuffer, 0, sizeInBytes);
                            }

                            // volume measuring
                            var channelSamples = new float[audioFrame.no_samples];
                            var maxValues = new double[audioFrame.no_channels];
                            for (int i = 0; i < audioFrame.no_channels; i++)
                            {
                                Marshal.Copy(audioFrame.p_data + (i * audioFrame.no_samples * sizeof(float)),
                                    channelSamples, 0, audioFrame.no_samples);
                                maxValues[i] = 20 * Math.Log10(channelSamples.Max(s => Math.Abs(s)));
                                if (maxValues[i] < MinAudioLevel)
                                    maxValues[i] = MinAudioLevel;
                            }
                            OnUiThread(() => SetAudioLevels(maxValues));
                        }
                        Ndi.NDIlib_recv_free_audio(recvInstance, ref audioFrame);
                        break;
                    case NDIlib_frame_type_e.NDIlib_frame_type_metadata:
                        Ndi.NDIlib_recv_free_metadata(recvInstance, ref metadataFrame);
                        break;
                }
            }
            Ndi.NDIlib_recv_destroy(recvInstance);
            Debug.WriteLine(this, "Receive thread exited");
        }

        private void SetAudioLevels(double[] maxValues)
        {
            if (AudioLevels.Length != maxValues.Length)
                AudioLevels = maxValues.Select(v => new AudioLevelBarViewmodel{AudioLevel = v}).ToArray();
            else
                for (var index = 0; index < maxValues.Length; index++)
                AudioLevels[index].AudioLevel = maxValues[index];
        }

        private void OnSourceRefreshed(object sender, EventArgs eventArgs)
        {
            var sources = _ndiSources;
            OnUiThread(() =>
            {
                // removing non-existing sources
                var notExistingSources = _videoSources
                    .Where(s => !(sources.ContainsKey(s) || s == Common.Properties.Resources._none_)).ToArray();
                foreach (var source in notExistingSources)
                    _videoSources.Remove(source);
                //adding new sources
                foreach (var source in sources)
                    if (!_videoSources.Contains(source.Key))
                        _videoSources.Add(source.Key);
            });
        }

        private void Disconnect()
        {
            if (_ndiReceiveThread != null)
            {
                _exitReceiveThread = true;
                _ndiReceiveThread.Join();
                _ndiReceiveThread = null;
            }
            _waveOut?.Dispose();
        }

    }
}
