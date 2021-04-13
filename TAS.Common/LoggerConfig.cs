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
#if NLOG_OUTPUT_ERROR
            var minLevel = "Error";
#elif NLOG_OUTPUT_WARNING
            var minLevel = "Warn";
#elif NLOG_OUTPUT_INFO
            var minLevel = "Info";
#elif NLOG_OUTPUT_DEBUG
            var minLevel = "Debug";
#elif NLOG_OUTPUT_TRACE
            var minLevel = "Trace";
#else
            var minLevel = "Fatal";
#endif
            var config = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();
            var debuggerTarget = new DebuggerTarget("Nlog");
            config.AddTarget(debuggerTarget);
            config.AddRule(LogLevel.FromString(minLevel), LogLevel.Fatal, debuggerTarget);
            LogManager.Configuration = config;
        }
    }
}
