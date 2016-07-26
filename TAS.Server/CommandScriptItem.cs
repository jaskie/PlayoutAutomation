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
        static string FILL_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        static Regex regexFill = new Regex(FILL_COMMAND, RegexOptions.IgnoreCase);
        static string CLIP_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        static Regex regexClip = new Regex(CLIP_COMMAND, RegexOptions.IgnoreCase);
        static string CLEAR_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLEAR\s*", string.Join("|", Enum.GetNames(typeof(VideoLayer))));
        static Regex regexClear = new Regex(CLEAR_COMMAND, RegexOptions.IgnoreCase);

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

        public override bool ValidateCommandText(string commandText)
        {
            return regexFill.IsMatch(commandText) 
                || regexClip.IsMatch(commandText)
                || regexClear.IsMatch(commandText);
        }

    }
}
