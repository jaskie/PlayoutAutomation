using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class CommandScriptItemProxy: ICommandScriptItem
    {
        public static string FILL_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        public static Regex regexFill = new Regex(FILL_COMMAND, RegexOptions.IgnoreCase);
        public static string CLIP_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        public static Regex regexClip = new Regex(CLIP_COMMAND, RegexOptions.IgnoreCase);
        public static string CLEAR_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLEAR\s*", string.Join("|", Enum.GetNames(typeof(VideoLayer))));
        public static Regex regexClear = new Regex(CLEAR_COMMAND, RegexOptions.IgnoreCase);

        public CommandScriptItemProxy() { }
        public CommandScriptItemProxy(ICommandScriptItem other)
        {
            ExecuteTime = other.ExecuteTime;
            Command = other.Command;
        }
        [DataMember]
        public TimeSpan? ExecuteTime { get; set; }
        [DataMember]
        public string Command { get; set; }
        public static bool ValidateCommandText(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText)
                && (regexFill.IsMatch(commandText)
                || regexClip.IsMatch(commandText)
                || regexClear.IsMatch(commandText));
        }
    }
}
