using NLog;
using NLog.Targets;
using System.Diagnostics;

namespace TAS.Common
{
    public static class LoggerConfig
    {
        [Conditional("DEBUG")]
        public static void AddDebuggerTarget()
        {
            var config = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
            var debuggerTarget = new DebuggerTarget("Nlog");
            config.AddTarget(debuggerTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debuggerTarget);
            LogManager.Configuration = config;
        }
    }
}
