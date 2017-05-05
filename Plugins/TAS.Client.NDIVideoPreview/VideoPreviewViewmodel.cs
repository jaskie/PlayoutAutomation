using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TAS.Client.Common;
using TAS.Client.NDIVideoPreview.Interop;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(Common.Plugin.IVideoPreview))]
    public class VideoPreviewViewmodel : ViewModels.ViewmodelBase, Common.Plugin.IVideoPreview
    {

        public ICommand CommandRefreshSources { get; private set; }
        public ICommand CommandGotoNdiWebsite { get; private set; }
        public ICommand CommandShowPopup { get; private set; }
        public ICommand CommandHidePopup { get; private set; }

        public VideoPreviewViewmodel()
        {
            View = new VideoPreviewView { DataContext = this };
            _videoSources = new ObservableCollection<string>(new []{ Common.Properties.Resources._none_ });
            CommandRefreshSources = new UICommand { ExecuteDelegate = RefreshSources, CanExecuteDelegate = o => _ndiFindInstance != IntPtr.Zero};
            CommandGotoNdiWebsite = new UICommand { ExecuteDelegate = GotoNdiWebsite };
            CommandShowPopup = new UICommand { ExecuteDelegate = o => DisplayPopup = true };
            CommandHidePopup = new UICommand { ExecuteDelegate = o => DisplayPopup = false };
            InitNdiFind();
            if (_ndiFindInstance != IntPtr.Zero)
            ThreadPool.QueueUserWorkItem(o =>
            {
                if (Ndi.NDIlib_find_wait_for_sources(_ndiFindInstance, 5000))
                {
                    Thread.Sleep(3000);
                    Application.Current.Dispatcher.BeginInvoke((Action) delegate { RefreshSources(null); });
                }
            });
        }



        #region IVideoPreview

        public UserControl View { get; private set; }

        public void SetSource(string sourceUrl)
        { 
            Application.Current.Dispatcher.BeginInvoke((Action)delegate { VideoSource = sourceUrl; });
        }

        #endregion IVideoPreview

        public IEnumerable<string> VideoSources { get { return _videoSources; } }

        public string VideoSource
        {
            get { return _videoSource; }
            set
            {
                if (SetField(ref _videoSource, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        Disconnect();
                        VideoBitmap = null;
                        DisplaySource = _ndiSources.ContainsKey(value);
                        if (DisplaySource)
                            ThreadPool.QueueUserWorkItem(o => Connect(value));
                    }
                }
            }
        }

        public bool DisplaySource { get { return _displaySource; } set { SetField(ref _displaySource, value); } }

        public WriteableBitmap VideoBitmap { get { return _videoBitmap; } private set { SetField(ref _videoBitmap, value); } }

        protected override void OnDispose()
        {
            Disconnect();
            if (_ndiFindInstance != IntPtr.Zero)
                Ndi.NDIlib_find_destroy(_ndiFindInstance);
        }

        // private members
        private Dictionary<string, NDIlib_source_t> _ndiSources;
        private ObservableCollection<string> _videoSources;
        private string _videoSource;
        private IntPtr _ndiFindInstance;
        private IntPtr _ndiReceiveInstance;
        private Thread _ndiReceiveThread = null;
        private volatile bool _exitReceiveThread = false;
        private WriteableBitmap _videoBitmap = null;
        private bool _displaySource;
        private bool _displayPopup;

        private void GotoNdiWebsite(object obj)
        {
            DisplayPopup = false;
            Process.Start(obj.ToString());
        }

        public bool DisplayPopup
        {
            get { return _displayPopup; }
            set { SetField(ref _displayPopup, value); }
        }


        private void RefreshSources(object obj)
        {
            if (_ndiFindInstance != IntPtr.Zero)
            {
                int numSources = 0;
                var ndi_sources = Ndi.NDIlib_find_get_current_sources(_ndiFindInstance, ref numSources);
                if (numSources > 0)
                {
                    int SourceSizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NDIlib_source_t));
                    Dictionary<string, NDIlib_source_t> sources = new Dictionary<string, NDIlib_source_t>();
                    for (int i = 0; i < numSources; i++)
                    {
                        IntPtr p = IntPtr.Add(ndi_sources, (i * SourceSizeInBytes));
                        NDIlib_source_t src = (NDIlib_source_t)System.Runtime.InteropServices.Marshal.PtrToStructure(p, typeof(NDIlib_source_t));
                        var ndiName = Ndi.Utf8ToString(src.p_ndi_name);
                        sources.Add(ndiName, src);
                        Debug.WriteLine($"Added source name:{Ndi.Utf8ToString(src.p_ndi_name)} address :{Ndi.Utf8ToString(src.p_ip_address)}");
                    }
                    // removing non-existing sources
                    var notExistingSources = _videoSources.Where(s => !(sources.ContainsKey(s) || s == Common.Properties.Resources._none_)).ToArray();
                    foreach (var source in notExistingSources)
                        _videoSources.Remove(source);
                    //adding new sources
                    foreach (var source in sources)
                        if (!_videoSources.Contains(source.Key))
                            _videoSources.Add(source.Key);
                    _ndiSources = sources;
                }
            }
        }

        private void InitNdiFind()
        {
            Ndi.AddRuntimeDir();
            var findDesc = new NDIlib_find_create_t()
            {
                p_groups = IntPtr.Zero,
                show_local_sources = true,
                p_extra_ips = IntPtr.Zero
            };
            _ndiFindInstance = Ndi.NDIlib_find_create2(ref findDesc);
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
                bandwidth = NDIlib_recv_bandwidth_e.NDIlib_recv_bandwidth_lowest
            };

            _ndiReceiveInstance = Ndi.NDIlib_recv_create(ref recvDescription);
            if (_ndiReceiveInstance != IntPtr.Zero)
            {
                // start up a thread to receive on
                _ndiReceiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "Newtek Ndi video preview plugin receive thread" };
                _ndiReceiveThread.Start();
            }
        }

        private void ReceiveThreadProc()
        {
            var recvInstance = _ndiReceiveInstance;
            if (recvInstance == IntPtr.Zero)
                return;
            while (!_exitReceiveThread)
            {
                NDIlib_video_frame_t videoFrame = new NDIlib_video_frame_t();
                NDIlib_audio_frame_t audioFrame = new NDIlib_audio_frame_t();
                NDIlib_metadata_frame_t metadataFrame = new NDIlib_metadata_frame_t();

                switch (Ndi.NDIlib_recv_capture(recvInstance, ref videoFrame, ref audioFrame, ref metadataFrame, 1000))
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
                        Application.Current?.Dispatcher.BeginInvoke(new Action(delegate
                        {

                            if (VideoBitmap == null
                                || VideoBitmap.PixelWidth != xres
                                || VideoBitmap.PixelHeight != yres)
                                VideoBitmap = new WriteableBitmap(xres, yres, 96, dpiY, System.Windows.Media.PixelFormats.Pbgra32, null);

                            // update the writeable bitmap
                            VideoBitmap.Lock();
                            VideoBitmap.WritePixels(new Int32Rect(0, 0, xres, yres), videoFrame.p_data, bufferSize, stride);
                            VideoBitmap.Unlock();
                            Ndi.NDIlib_recv_free_video(recvInstance, ref videoFrame);
                        }));
                        break;
                    case NDIlib_frame_type_e.NDIlib_frame_type_audio:
                        Ndi.NDIlib_recv_free_audio(recvInstance, ref audioFrame);
                        break;
                    case NDIlib_frame_type_e.NDIlib_frame_type_metadata:
                        Ndi.NDIlib_recv_free_metadata(recvInstance, ref metadataFrame);
                        break;

                }

            }
            Debug.WriteLine(this, "Receive thread exited");
        }

        private void Disconnect()
        {
            if (_ndiReceiveThread != null)
            {
                _exitReceiveThread = true;
                _ndiReceiveThread.Join(1000);
            }
            _ndiReceiveThread = null;
            _exitReceiveThread = false;
            Ndi.NDIlib_recv_destroy(_ndiReceiveInstance);
            _ndiReceiveInstance = IntPtr.Zero;
        }


    }
}
