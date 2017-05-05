using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class Recorder : ProxyBase, IRecorder
    {
        #region IRecorder
        [JsonProperty(nameof(IRecorder.Channels))]
        private List<PlayoutServerChannel> _channels { get { return Get<List<PlayoutServerChannel>>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<IPlayoutServerChannel> Channels { get { return _channels; } }

        public TimeSpan CurrentTc { get { return Get<TimeSpan>(); }  set { SetLocalValue(value); } }

        public TDeckControl DeckControl { get { return Get<TDeckControl>(); } set { SetLocalValue(value); } }

        public TDeckState DeckState { get { return Get<TDeckState>(); } set { SetLocalValue(value); } }
        
        public int Id { get { return Get<int>(); } set { SetLocalValue(value); } }

        public bool IsDeckConnected { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public bool IsServerConnected { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public string RecorderName { get { return Get<string>(); } set { SetLocalValue(value); } }
        
        public IMediaDirectory RecordingDirectory { get { return Get<MediaDirectory>(); } set { SetLocalValue(value); } }

        public IMedia RecordingMedia { get { return Get<IMedia>(); } set { SetLocalValue(value); } }

        public TimeSpan TimeLimit { get { return Get<TimeSpan>(); } set { SetLocalValue(value); } }

        public TimeSpan CaptureTimeLimit { get { return Get<TimeSpan>(); } set { SetLocalValue(value); } }

        public TimeSpan CaptureTcIn { get { return Get<TimeSpan>(); } set { SetLocalValue(value); } }

        public TimeSpan CaptureTcOut { get { return Get<TimeSpan>(); } set { SetLocalValue(value); } }

        public bool CaptureNarrowMode { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public IPlayoutServerChannel CaptureChannel { get { return Get<PlayoutServerChannel>(); } set { SetLocalValue(value); } }

        public string CaptureFileName { get { return Get<string>(); } set { SetLocalValue(value); } }

        public void Abort() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, bool narrowMode, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, tcIn, tcOut, narrowMode, fileName });
        }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, bool narrowMode, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, narrowMode, fileName });
        }

        public void DeckFastForward() { Invoke(); }

        public void GoToTimecode(TimeSpan tc, TVideoFormat format) { Invoke(parameters: new { tc, format }); }
        
        public void DeckRewind() { Invoke(); }

        public void DeckStop() { Invoke(); }

        public void DeckPlay() { Invoke(); }

        public void Finish() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, fileName });
        }

        public void SetTimeLimit(TimeSpan limit) { Invoke(parameters: new object[] { limit }); }
        #endregion IRecorder

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
