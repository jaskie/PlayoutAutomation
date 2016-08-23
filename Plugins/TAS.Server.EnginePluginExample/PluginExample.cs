using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server.EnginePluginExample
{
    [Export(typeof(IEnginePlugin))]
    public class PluginExample : IEnginePlugin
    {
        private IEngine _engine;
        public void Initialize(IEngine engine)
        {
            _engine = engine;
            _engine.EngineOperation += _engine_EngineOperation;
        }

        private void _engine_EngineOperation(object sender, EngineOperationEventArgs e)
        {
            if (e.Operation == TAS.Common.TEngineOperation.Play)
            {
                IServerMedia media = e.Event?.Media as IServerMedia;
                if (media == null)
                    return;
                if (!string.IsNullOrWhiteSpace(media.IdAux) || media.MediaCategory == TAS.Common.TMediaCategory.Commercial)
                {
                    UriBuilder uri = new UriBuilder(Properties.Settings.Default.URL);
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("playoutServerTimestamp", (e.Event.StartTime.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                    if (!string.IsNullOrWhiteSpace(media.IdAux))
                        parameters.Add("eventType", media.IdAux);
                    else
                        parameters.Add("eventType", "STARTING");
                    parameters.Add("eventDuration", ((int)e.Event.Duration.TotalMilliseconds).ToString());
                    uri.Query = string.Join("&", parameters.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri.Uri);
                    req.Method = "POST";
                    using (var writer = new System.IO.StreamWriter(req.GetRequestStream()))
                    {
                        new Newtonsoft.Json.JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented, PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None }
                        .Serialize(writer, EventProxy.FromEvent(e.Event));
                    }
                    req.BeginGetResponse(null, null);
                }
            }
        }
    }
}
