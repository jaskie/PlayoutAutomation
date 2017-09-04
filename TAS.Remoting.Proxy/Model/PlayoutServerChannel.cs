using System;
using Newtonsoft.Json;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServerChannel : ProxyBase, IPlayoutServerChannel
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IPlayoutServerChannel.ChannelName))]
        private string _channelName;

        [JsonProperty(nameof(IPlayoutServerChannel.Id))]
        private int _id;

        [JsonProperty(nameof(IPlayoutServerChannel.IsServerConnected))]
        private bool _isServerConnected;

        [JsonProperty(nameof(IPlayoutServerChannel.VideoFormat))]
        private TVideoFormat _videoFormat;

        [JsonProperty(nameof(IPlayoutServerChannel.AudioLevel))]
        private int _audioLevel;

        [JsonProperty(nameof(IPlayoutServerChannel.PreviewUrl))]
        private string _previewUrl;

        #pragma warning restore

        public string ChannelName => _channelName;

        public int Id => _id;

        public bool IsServerConnected => _isServerConnected;

        public TVideoFormat VideoFormat => _videoFormat;

        public int AudioLevel => _audioLevel;

        public string PreviewUrl => _previewUrl;

        public void Clear()
        {
            Invoke();
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            Invoke(parameters: new[] { aVideoLayer });
        }

        public bool Load(IEventPesistent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Load(System.Drawing.Color color, VideoLayer layer)
        {
            throw new NotImplementedException();
        }

        public bool Load(IMedia media, VideoLayer videolayer, long seek, long duration)
        {
            throw new NotImplementedException();
        }

        public bool LoadNext(IEventPesistent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Pause(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public bool Pause(IEventPesistent aEvent)
        {
            throw new NotImplementedException();
        }

        public bool Play(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public bool Play(IEventPesistent aEvent)
        {
            throw new NotImplementedException();
        }

        public void ReStart(IEventPesistent ev)
        {
            throw new NotImplementedException();
        }

        public bool Seek(VideoLayer videolayer, long position)
        {
            throw new NotImplementedException();
        }

        public void SetAspect(VideoLayer layer, bool narrow)
        {
            throw new NotImplementedException();
        }

        public void SetVolume(VideoLayer videolayer, double volume)
        {
            throw new NotImplementedException();
        }

        public bool Stop(VideoLayer videolayer)
        {
            throw new NotImplementedException();
        }

        public bool Stop(IEventPesistent aEvent)
        {
            throw new NotImplementedException();
        }

        protected override void OnEventNotification(WebSocketMessage message) { }

    }
}
