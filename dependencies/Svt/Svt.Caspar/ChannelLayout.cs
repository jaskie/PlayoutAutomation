using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Svt.Caspar
{
    public enum ChannelLayout
    {
        Default,
        Mono,
        Stereo,
        DualStereo,
        DTS,
        DolbyE,
        DolbyDigital,
        SMPTE,
        Passthru
    }

    public static class ChannelLayoutExtensions
    {
        public static string ToAMCPChannelLayout(this ChannelLayout channelLayout)
        {
            switch (channelLayout)
            {
                case ChannelLayout.Mono:
                    return "MONO";
                case ChannelLayout.Stereo:
                    return "STEREO";
                case ChannelLayout.DualStereo:
                    return "DUAL-STEREO";
                case ChannelLayout.DTS:
                    return "DTS";
                case ChannelLayout.DolbyE:
                    return "DOLBYE";
                case ChannelLayout.DolbyDigital:
                    return "DOLBYDIGITAL";
                case ChannelLayout.SMPTE:
                    return "SMPTE";
                case ChannelLayout.Passthru:
                    return "PASSTHRU";
                default:
                    return string.Empty;
            }
        }
    }
}
