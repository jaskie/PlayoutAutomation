using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.NDIVideoPreview.Interop
{
    public enum NDIlib_frame_type_e
    {
        NDIlib_frame_type_none = 0,
        NDIlib_frame_type_video = 1,
        NDIlib_frame_type_audio = 2,
        NDIlib_frame_type_metadata = 3,
        NDIlib_frame_type_error = 4
    }

    public enum NDIlib_frame_format_type_e
    {
        NDIlib_frame_format_type_interleaved = 0,
        NDIlib_frame_format_type_progressive = 1,
        NDIlib_frame_format_type_field_0 = 2,
        NDIlib_frame_format_type_field_1 = 3
    }

    public enum NDIlib_FourCC_type_e : uint
    {
        NDIlib_FourCC_type_UYVY = 0x59565955U, //"YVYU"
        NDIlib_FourCC_type_UYVA = 0x41565955U, //"AVYU"
        NDIlib_FourCC_type_BGRA = 0x41524742U, //...
        NDIlib_FourCC_type_BGRX = 0x58524742U,
        NDIlib_FourCC_type_RGBA = 0x41424752U,
        NDIlib_FourCC_type_RGBX = 0x58424752U
    }


    #region Recv
    public enum NDIlib_recv_bandwidth_e : uint
    {
        NDIlib_recv_bandwidth_audio_only = 10, // Receive only audio.
        NDIlib_recv_bandwidth_lowest = 0,  // Receive video at a lower bandwidth and resolution.
        NDIlib_recv_bandwidth_highest = 100 // Default.
    }

    public enum NDIlib_recv_color_format_e
    {
        NDIlib_recv_color_format_e_BGRX_BGRA = 0,	// No alpha channel: BGRX, Alpha channel: BGRA
        NDIlib_recv_color_format_e_UYVY_BGRA = 1,	// No alpha channel: UYVY, Alpha channel: BGRA
        NDIlib_recv_color_format_e_RGBX_RGBA = 2,	// No alpha channel: RGBX, Alpha channel: RGBA
        NDIlib_recv_color_format_e_UYVY_RGBA = 3	// No alpha channel: UYVY, Alpha channel: RGBA
    }

    #endregion Recv
}
