using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    //[XmlRoot("NowPlaying")]
    public class NowPlayingNotifier: IEnginePlugin
    {
        private IEngine _engine;
        private int _isInitialized;

        public void Initialize(IEngine engine)
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) != default(int))
                return;
            _engine = engine;
            _engine.EngineOperation += _engine_EngineOperation;
        }

        public string CommandOnPlay { get; set; }

        [XmlAttribute]
        public string Engine { get; set; }

        private void _engine_EngineOperation(object sender, EngineOperationEventArgs e)
        {
            Debug.WriteLine($"Plugin notification received for {e.Event}");
            if (e.Operation == TEngineOperation.Load)
            {
                
            }
            //if (e.Operation == TAS.Common.TEngineOperation.Play)
            //{
            //    IServerMedia media = e.Event?.Media as IServerMedia;
            //    if (media == null)
            //        return;
            //    if (!string.IsNullOrWhiteSpace(media.IdAux) || media.MediaCategory == TAS.Common.TMediaCategory.Commercial)
            //    {
            //        UriBuilder uri = new UriBuilder(@"http://localhost/live/app/registerEvent.php");
            //        Dictionary<string, string> parameters = new Dictionary<string, string>();
            //        parameters.Add("playoutServerTimestamp", (e.Event.StartTime.Ticks / TimeSpan.TicksPerMillisecond).ToString());
            //        if (!string.IsNullOrWhiteSpace(media.IdAux))
            //            parameters.Add("eventType", media.IdAux);
            //        else
            //            parameters.Add("eventType", "STARTING");
            //        parameters.Add("eventDuration", ((int)e.Event.Duration.TotalMilliseconds).ToString());
            //        uri.Query = string.Join("&", parameters.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
            //        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri.Uri);
            //        req.Proxy = null;
            //        using (MemoryStream jsonData = new MemoryStream())
            //        using (var writer = new System.IO.StreamWriter(jsonData))
            //        {
            //            new Newtonsoft.Json.JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented, PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None }
            //            .Serialize(writer, EventProxy.FromEvent(e.Event));
            //            req.Method = "POST";
            //            req.ContentLength = jsonData.Length;
            //            req.ContentType = "application/json";
            //            Stream reqStream = req.GetRequestStream();
            //            jsonData.WriteTo(reqStream);
            //            reqStream.Close();
            //        }
            //        Debug.WriteLine(req.GetResponse());
            //    }
            //}
        }
    }
}
