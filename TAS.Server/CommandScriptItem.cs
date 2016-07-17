using Svt.Caspar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server
{
    public class CommandScriptItem : CommandScriptItemBase
    {
        static string FILL_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]*)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetValues(typeof(VideoLayer))), string.Join("|", Enum.GetValues(typeof(TEasing))));
        static Regex regexFill = new Regex(FILL_COMMAND);
        static string CLEAR_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLEAR\s*", string.Join("|", Enum.GetValues(typeof(VideoLayer))));
        static Regex regexClear = new Regex(CLEAR_COMMAND);

        internal bool Execute(Svt.Caspar.Channel channel)
        {
            string command = Command;
            Match match = regexFill.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value);
                float x = float.Parse(match.Groups["x"].Value);
                float y = float.Parse(match.Groups["y"].Value);
                float sx = float.Parse(match.Groups["sx"].Value);
                float sy = float.Parse(match.Groups["sy"].Value);
                int duration = match.Groups["duration"].Success ? int.Parse(match.Groups["duration"].Value) : 0;
                TEasing easing = match.Groups["easing"].Success ? (TEasing)Enum.Parse(typeof(TEasing), match.Groups["easing"].Value) : TEasing.Linear;
                channel.Fill((int)layer, x, y, sx, sy, duration, (Easing)easing);
                return true;
            }
            match = regexClear.Match(command);
            if (match.Success)
            {
                VideoLayer layer = (VideoLayer)Enum.Parse(typeof(VideoLayer), match.Groups["layer"].Value);
                channel.ClearMixer((int)layer);
                return true;
            }
            return false;
        }
        internal bool IsExecuted = false;
        
        

    }
}
