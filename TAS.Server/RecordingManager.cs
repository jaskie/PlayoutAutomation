using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class RecordingManager
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private IList<CasparServer> _servers;
        private IEngine _engine;
        public IRecorder Recorder { get; private set; }
        public IEvent Recorded { get; private set; }
        public RecordingManager(IEngine engine, IList<CasparServer> servers)
        {
            _servers = servers;
            _engine = engine;
        }

        public void Capture(IEvent @event)
        {
            try
            {
                var server = _servers.FirstOrDefault(s => (int)s.Id == @event.RecordingInfo.ServerId);
                var recorder = server?.Recorders.FirstOrDefault(r => r.Id == @event.RecordingInfo.RecorderId);
                if (recorder == null || recorder.RecordingMedia != null)
                    return;
                var name = String.Concat(@event.EventName, '_', DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss"));
                recorder.Capture(recorder.Channels.FirstOrDefault(c => c.Id == @event.RecordingInfo.ChannelId), @event.Duration>new TimeSpan(2, 0, 0) ? @event.Duration : new TimeSpan(2, 0, 0), _engine.FormatDescription.IsWideScreen, name, String.Concat(name, '.', server.MovieContainerFormat), null);
                Recorder = recorder;
                Recorded = @event;
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error when starting recording.");
            }            
        }

        public void Stop()
        {
            Recorder.Finish();
            Recorder = null;
        }
    }
}
