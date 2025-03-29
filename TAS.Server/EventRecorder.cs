using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    internal class EventRecorder
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IReadOnlyCollection<CasparServer> _servers;
        private IEngine _engine;
        public IRecorder Recorder { get; private set; }
        public IEvent Recording { get; private set; }
        public EventRecorder(IEngine engine, IReadOnlyCollection<CasparServer> servers)
        {
            _servers = servers;
            _engine = engine;
        }

        public void StartCapture(IEvent @event)
        {
            try
            {
                var server = _servers.FirstOrDefault(s => (int)s.Id == @event.RecordingInfo.ServerId);
                var recorder = server?.Recorders.FirstOrDefault(r => r.Id == @event.RecordingInfo.RecorderId);
                if (recorder == null || recorder.RecordingMedia != null)
                    return;
                var name = string.Concat(@event.EventName, '_', DateTime.Now.ToString("s").Replace(':', '_'));
                recorder.Capture(recorder.Channels.FirstOrDefault(c => c.Id == @event.RecordingInfo.ChannelId), @event.Duration>new TimeSpan(2, 0, 0) ? @event.Duration : new TimeSpan(2, 0, 0), _engine.FormatDescription.IsWideScreen, name, string.Concat(name, '.', server.MovieContainerFormat), null);
                Recorder = recorder;
                Recording = @event;
            }
            catch(Exception ex)
            {
                Logger.Error(ex, "Error when starting recording.");
            }
        }

        public void EndCapture()
        {
            Recorder?.Finish();
            Recorder = null;
            Recording = null;
        }
    }
}
