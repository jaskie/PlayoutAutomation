using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using TAS.Common;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    [Newtonsoft.Json.JsonObject(IsReference = false)]
    public class CommandScriptItemBase : DtoBase, ICommandScriptItem
    {

        protected static string FILL_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+FILL\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        protected static Regex regexFill = new Regex(FILL_COMMAND, RegexOptions.IgnoreCase);
        protected static string CLIP_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLIP\s+(?<x>[+-]?([0-9]*[.])?[0-9]+)\s+(?<y>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sx>[+-]?([0-9]*[.])?[0-9]+)\s+(?<sy>[+-]?([0-9]*[.])?[0-9]+)(\s+(?<duration>([0-9]+)))?(\s+(?<easing>({1})))?", string.Join("|", Enum.GetNames(typeof(VideoLayer))), string.Join("|", Enum.GetNames(typeof(TEasing))));
        protected static Regex regexClip = new Regex(CLIP_COMMAND, RegexOptions.IgnoreCase);
        protected static string CLEAR_COMMAND = string.Format(@"\s*MIXER\s+(?<layer>{0})\s+CLEAR\s*", string.Join("|", Enum.GetNames(typeof(VideoLayer))));
        protected static Regex regexClear = new Regex(CLEAR_COMMAND, RegexOptions.IgnoreCase);

        public CommandScriptItemBase()
        { }

        public CommandScriptItemBase(ICommandScriptItem other)
        {
            _executeTime = other.ExecuteTime;
            _command = other.Command;
        }

        private TimeSpan? _executeTime;
        [DataMember]
        public TimeSpan? ExecuteTime
        {
            get { return _executeTime; }
            set { SetField(ref _executeTime, value, "ExecuteTime"); }
        }

        private string _command;
        [DataMember]
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value, "Command"); }
        }

        public bool ValidateCommandText(string commandText)
        {
            return !string.IsNullOrWhiteSpace(commandText)
                && (regexFill.IsMatch(commandText)
                || regexClip.IsMatch(commandText)
                || regexClear.IsMatch(commandText));
        }
    }
}
