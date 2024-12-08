using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TAS.Client.NDIVideoPreview.Model;
using TAS.Client.NDIVideoPreview.Interop;
using NLog;

namespace TAS.Client.NDIVideoPreview
{
    public class VideoPreviewViewmodel : ViewModelBase, Common.Plugin.IVideoPreview
    {
        private const double MinAudioLevel = -60;
        private readonly ObservableCollection<string> _videoSources;
        private string _videoSource;
        private ThreadStartParameters _currentThreadParameters;
        private WriteableBitmap _videoBitmap;
        private int _videoBitmapWidth, _videoBitmapHeight;
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

        // static fields
        private static NdiSourcesWatcher NdiSourcesWatcher;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static VideoPreviewViewmodel()
        {
            Ndi.AddRuntimeDir();
            NdiSourcesWatcher = new NdiSourcesWatcher();
        }

        public VideoPreviewViewmodel()
        {
            _videoSources = new ObservableCollection<string>(new[] { Common.Properties.Resources._none_ });
            _videoSource = _videoSources.FirstOrDefault();
            CommandRefreshSources = new UiCommand(CommandName(nameof(RefreshSources)), RefreshSources);
            CommandGotoNdiWebsite = new UiCommand(CommandName(nameof(GotoNdiWebsite)), GotoNdiWebsite);
            CommandShowPopup = new UiCommand(CommandName(nameof(CommandShowPopup)), o => DisplayPopup = true);
            CommandHidePopup = new UiCommand(CommandName(nameof(CommandHidePopup)), o => DisplayPopup = false);
            NdiSourcesWatcher.SourceAdded += OnSourceAdded;
            NdiSourcesWatcher.SourceRemoved += OnSourceRemoved;
            RefreshAudioDevices();
            View = new VideoPreviewView { DataContext = this };
            OnUiThread(() =>
            {
                foreach (var source in NdiSourcesWatcher.GetSources())
                    if (!_videoSources.Contains(source))
                        _videoSources.Add(source);
            });
        }

        public ICommand CommandRefreshSources { get; }
        public ICommand CommandGotoNdiWebsite { get; }
        public ICommand CommandShowPopup { get; }
        public ICommand CommandHidePopup { get; }

        #region IVideoPreview

        public UserControl View { get; }

        /// <summary>
        /// Method accepts address in form ndi://ip_address:port and ndi://machine_name:ndi_name
        /// </summary>
        /// <param name="sourceUrl"></param>
        public void SetSource(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return;
            if (sourceUrl.StartsWith("ndi://"))
            {
                var source = NdiSourcesWatcher.FindSource(sourceUrl.Substring(6));
                if (string.IsNullOrEmpty(source.Key))
                    return;
                OnUiThread(() =>
                {
                    VideoSource = source.Key;
                });
            }
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
                        AudioLevels = Array.Empty<AudioLevelBarViewmodel>();
                        Connect(value);
                    }
                }
            }
        }

        public WriteableBitmap VideoBitmap
        {
            get => _videoBitmap;
            private set
            {
                if (!SetField(ref _videoBitmap, value))
                    return;
                _videoBitmapWidth = value?.PixelWidth ?? 0;
                _videoBitmapHeight = value?.PixelHeight ?? 0;
                NotifyPropertyChanged(nameof(IsDisplayAudioBars));
            }
        }

        public bool IsDisplayAudioBars
        {
            get => _isDisplayAudioBars && _videoBitmap != null;
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
            NdiSourcesWatcher.SourceAdded -= OnSourceAdded;
            NdiSourcesWatcher.SourceRemoved -= OnSourceRemoved;
        }

        private void GotoNdiWebsite(object obj)
        {
            var address = obj as string ?? throw new ArgumentException("Expected string", nameof(obj));
            DisplayPopup = false;
            System.Diagnostics.Process.Start(address);
        }

        public bool DisplayPopup
        {
            get => _displayPopup;
            set => SetField(ref _displayPopup, value);
        }

        private async void RefreshSources(object _)
        {
            try
            {
                RefreshAudioDevices();
                await Task.Run(() =>
                {
                    NdiSourcesWatcher.RefreshSources();
                });
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
            var source = NdiSourcesWatcher.FindSource(sourceName);
            if (source.Value is null || _currentThreadParameters != null)
                return;
            NDIlib_recv_create_t recvDescription = new NDIlib_recv_create_t
            {
                source_to_connect_to = new NDIlib_source_t
                {
                    p_ndi_name = Ndi.StringToUtf8(source.Key),
                    p_ip_address = Ndi.StringToUtf8(source.Value)
                },
                color_format = NDIlib_recv_color_format_e.NDIlib_recv_color_format_e_BGRX_BGRA,
                bandwidth = NDIlib_recv_bandwidth_e.NDIlib_recv_bandwidth_lowest,
                allow_video_fields = false
            };

            var ndiReceiveInstance = Ndi.NDIlib_recv_create(ref recvDescription);
            if (ndiReceiveInstance == default)
                return;
            // start up a thread to receive on
            _currentThreadParameters = new ThreadStartParameters { NdiReceiveInstance = ndiReceiveInstance, SourceName = sourceName };
            new Thread(ReceiveThreadProc) { IsBackground = true, Name = $"Newtek Ndi video preview plugin receive thread for {sourceName}" }
                .Start(_currentThreadParameters);
        }

        private void ReceiveThreadProc(object parameters)
        {
            var threadParameters = parameters as ThreadStartParameters ?? throw new ArgumentException("Invalid parameter provided to threadStart");
            if (threadParameters?.NdiReceiveInstance == default)
                return;
            var audioDevice = SelectedAudioDevice;
            while (!threadParameters.ExitThread)
            {
                NDIlib_video_frame_t videoFrame = new NDIlib_video_frame_t();
                NDIlib_audio_frame_t audioFrame = new NDIlib_audio_frame_t();
                NDIlib_metadata_frame_t metadataFrame = new NDIlib_metadata_frame_t();

                switch (Ndi.NDIlib_recv_capture(threadParameters.NdiReceiveInstance, ref videoFrame, ref audioFrame, ref metadataFrame, 100))
                {
                    case NDIlib_frame_type_e.NDIlib_frame_type_video:
                        if (videoFrame.p_data != IntPtr.Zero && !threadParameters.ExitThread)
                        {
                            int yres = (int)videoFrame.yres;
                            int xres = (int)videoFrame.xres;

                            var videoBitmap = VideoBitmap;
                            if (videoBitmap == null || _videoBitmapWidth != xres || _videoBitmapHeight != yres)
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    double dpiY = 96.0 * (videoFrame.picture_aspect_ratio / ((double)xres / yres));
                                    VideoBitmap = new WriteableBitmap(xres, yres, 96, dpiY, System.Windows.Media.PixelFormats.Pbgra32, null);
                                });
                                videoBitmap = VideoBitmap;
                            }
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                if (videoBitmap.TryLock(TimeSpan.FromMilliseconds(100)))
                                {
                                    uint bufferSize = videoFrame.yres * videoFrame.line_stride_in_bytes;
                                    videoBitmap.WritePixels(new Int32Rect(0, 0, xres, yres), videoFrame.p_data, (int)bufferSize, (int)videoFrame.line_stride_in_bytes);
                                    videoBitmap.Unlock();
                                }
                            });
                        }
                        Ndi.NDIlib_recv_free_video(threadParameters.NdiReceiveInstance, ref videoFrame);
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
                            SetAudioLevels(maxValues);
                        }
                        Ndi.NDIlib_recv_free_audio(threadParameters.NdiReceiveInstance, ref audioFrame);
                        break;
                    case NDIlib_frame_type_e.NDIlib_frame_type_metadata:
                        Ndi.NDIlib_recv_free_metadata(threadParameters.NdiReceiveInstance, ref metadataFrame);
                        break;
                }
            }
            Ndi.NDIlib_recv_destroy(threadParameters.NdiReceiveInstance);
            Logger.Debug("NDI receive thread for {0} exited", threadParameters.SourceName);
        }

        private void SetAudioLevels(double[] maxValues)
        {
            if (AudioLevels.Length != maxValues.Length)
                AudioLevels = maxValues.Select(v => new AudioLevelBarViewmodel { AudioLevel = v }).ToArray();
            else
                for (var index = 0; index < maxValues.Length; index++)
                    AudioLevels[index].AudioLevel = maxValues[index];
        }

        private void OnSourceRemoved(object sender, NdiSourceEventArgs eventArgs)
        {
            OnUiThread(() => _videoSources.Remove(eventArgs.SourceName));
        }

        private void OnSourceAdded(object sender, NdiSourceEventArgs eventArgs)
        {
            OnUiThread(() =>
            {
                if (!_videoSources.Contains(eventArgs.SourceName))
                    _videoSources.Add(eventArgs.SourceName);
            });
        }

        private void Disconnect()
        {
            var threadParameters = _currentThreadParameters;
            if (threadParameters != null)
            {
                threadParameters.ExitThread = true;
                _currentThreadParameters = null;
            }
            _waveOut?.Dispose();
        }

        private class ThreadStartParameters
        {
            public IntPtr NdiReceiveInstance;
            public volatile bool ExitThread;
            public string SourceName;
        }
    }
}
