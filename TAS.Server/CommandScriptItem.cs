using Svt.Caspar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server
{
    public class CommandScriptItem : CommandScriptItemBase
    {
        internal bool Execute(Svt.Caspar.Channel channel)
        {
            string command = Command;
            Match match = regexFill.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, System.Globalization.CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value) ? 0 : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success ? (TEasing)Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true) : TEasing.Linear;
                channel.Fill((int)layer, x, y, sx, sy, duration, (Easing)easing);
                return true;
            }
            match = regexClip.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                float x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sx = float.Parse(match.Groups["sx"].Value, System.Globalization.CultureInfo.InvariantCulture);
                float sy = float.Parse(match.Groups["sy"].Value, System.Globalization.CultureInfo.InvariantCulture);
                int duration = string.IsNullOrWhiteSpace(match.Groups["duration"].Value) ? 0 : int.Parse(match.Groups["duration"].Value);
                TEasing easing = match.Groups["easing"].Success ? (TEasing)Enum.Parse(typeof(TEasing), match.Groups["easing"].Value, true) : TEasing.Linear;
                channel.Clip((int)layer, x, y, sx, sy, duration, (Easing)easing);
                return true;
            }
            match = regexClear.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value, true);
                channel.ClearMixer((int)layer);
                return true;
            }
            return false;
        }

        internal bool IsExecuted = false;
    }
}
