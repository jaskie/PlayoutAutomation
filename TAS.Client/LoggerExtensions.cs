using TAS.Client.ViewModels;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    internal static class LoggerExtensions
    {
        public static void LogEventDeletion(this NLog.Logger logger, IEvent evt)
        {
            const string messageToLog = "Deleted rundown item: {0} - {1}";
            switch (evt.EventType)
            {
                case TAS.Common.TEventType.Rundown:
                case TAS.Common.TEventType.Container:
                    logger.Info(messageToLog, evt.EventType, evt.EventName);
                    break;
                default:
                    logger.Debug(messageToLog, evt.EventType, evt.EventName);
                    break;
            }
        }
    }
}
