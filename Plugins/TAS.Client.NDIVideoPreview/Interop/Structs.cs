using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.NDIVideoPreview.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NDIlib_source_t
    {
        /// <summary>
        /// A UTF8 string that provides a user readable name for this source.
        /// This can be used for serialization, etc... and comprises the machine
        /// name and the source name on that machine. In the form
        /// 	MACHINE_NAME (NDI_SOURCE_NAME)
        /// If you specify this parameter either as NULL, or an EMPTY string then the 
        /// specific ip addres adn port number from below is used.
        /// </summary>
        public IntPtr p_ndi_name;

        /// <summary>
        /// A UTF8 string that provides the actual IP address and port number. 
        /// This is in the form : IP_ADDRESS:PORT_NO, for instance "127.0.0.1:10000"
        /// If you leave this parameter either as NULL, or an EMPTY string then the 
        /// ndi name above is used to look up the mDNS name to determine the IP and 
        /// port number. Connection is faster if the IP address is known.
        /// </summary>
        public IntPtr p_ip_address;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_video_frame_t
    {
        /// <summary>
        /// Horizontal resolution of this frame
        /// </summary>
        public uint xres;

        /// <summary>
        /// Vertical resolution of this frame
        /// </summary>
        public uint yres;

        /// <summary>
        /// What FourCC this is with. This can be two values
        /// </summary>
        public NDIlib_FourCC_type_e FourCC;

        /// <summary>
        /// Frame-rate numerator part of this frame. 
        /// </summary>
        public uint frame_rate_N;

        /// <summary>
        /// Frame-rate denominator part of this frame. 
        /// </summary>
        public uint frame_rate_D;

        /// <summary>
        /// Picture aspect ratio of this frame.
        /// </summary>
        public float picture_aspect_ratio;

        /// <summary>
        /// Is this a fielded frame, or is it progressive
        /// </summary>
        public NDIlib_frame_format_type_e frame_format_type;

        /// <summary>
        /// Timecode of this frame in 100ns intervals 
        /// </summary>
        public long timecode;

        /// <summary>
        /// Video data itself
        /// </summary>
        public IntPtr p_data;

        /// <summary>
        /// Inter line stride of the video data, in bytes.
        /// </summary>
        public uint line_stride_in_bytes;
    }


    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_audio_frame_t
    {
        /// <summary>
        /// Sample-rate of this buffer
        /// </summary>
        public uint sample_rate;

        /// <summary>
        /// Number of audio channels
        /// </summary>
        public uint no_channels;

        /// <summary>
        /// Number of audio samples per channel
        /// </summary>
        public uint no_samples;

        /// <summary>
        /// Timecode of this frame in 100ns intervals
        /// </summary>
        public long timecode;

        /// <summary>
        /// Audio data, float[] type
        /// </summary>
        public IntPtr p_data;

        // The inter channel stride of the audio channels, in bytes
        public uint channel_stride_in_bytes;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_metadata_frame_t
    {
        /// <summary>
        /// Length of the metadata in UTF8 characters. This includes the NULL terminating character.
        /// </summary>
        public uint length;

        /// <summary>
        /// Timecode of this frame in 100ns intervals
        /// </summary>
        public long timecode;

        /// <summary>
        /// Metadata as a UTF8 XML string. This is a NULL terminated string.
        /// </summary>
        public IntPtr p_data;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_tally_t
    {
        /// <summary>
        /// Is this currently on program output
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool on_program;

        /// <summary>
        /// Is this currently on preview output
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool on_preview;
    }

    #region Find
    /// <summary>
    /// Structure that is used to create a finder
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_find_create_t
    {
        /// <summary>
        /// Determines visibility of NDI sources running on local machine
        /// </summary>
        [MarshalAsAttribute(UnmanagedType.U1)]
        public bool show_local_sources;

        /// <summary>
        /// Roles to search for sources. UTF-8 string
        /// </summary>
        public IntPtr p_groups;

        /// <summary>
        /// UTF-8 string containing comma-separated list of additional IP addresses that should be quried for 
        /// sources.
        /// When not specified (IntPtr.Zero), the registry is used.
        /// </summary>
        public IntPtr p_extra_ips;
    }
    #endregion Find

    #region Recv
    /// <summary>
    /// Receiver creation structure
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_recv_create_t
    {
        /// <summary>
        /// The source that you wish to connect to.
        /// </summary>
        public NDIlib_source_t source_to_connect_to;

        /// <summary>
        /// Preffered color space.
        /// </summary>
        public NDIlib_recv_color_format_e color_format;

        /// <summary>
        /// Bandwidth setting that you wish to use for this video source. Bandwidth
        /// controlled by changing both the compression level and the resolution of the source.
        ///  A good use for low bandwidth is working on WIFI connections. 
        ///  </summary>
        public NDIlib_recv_bandwidth_e bandwidth;

        /// <summary>
        /// When this flag is FALSE, all video that you receive will be progressive. For sources
        /// that provide fields, this is de-interlaced on the receiving side (because we cannot change
        /// what the up-stream source was actually rendering. This is provided as a convenience to
        /// down-stream sources that do not wish to understand fielded video. There is almost no 
        /// performance impact of using this function.
        /// </summary>
        public bool allow_video_fields;
    }

    /// <summary>
    /// This allows you determine the current performance levels of the receiving to be able to detect whether frames have been dropped
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_recv_performance_t
    {
        /// <summary>
        /// Number of video frames
        /// </summary>
        long m_video_frames;

        /// <summary>
        /// Number of audio frames
        /// </summary>
        long m_audio_frames;

        /// <summary>
        /// Number of metadata frames
        /// </summary>
        long m_metadata_frames;
    }


    /// <summary>
    /// Current queue depths
    /// </summary>
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NDIlib_recv_queue_t
    {
        /// <summary>
        /// Number of video frames
        /// </summary>
        public long m_video_frames;

        /// <summary>
        /// Number of audio frames
        /// </summary>
        public long m_audio_frames;

        /// <summary>
        /// Number of metadata frames
        /// </summary>
        public long m_metadata_frames;
    };
    #endregion Recv

}
