using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class PlayoutServerChannel : ProxyBase, IPlayoutServerChannel
    {
        public string ChannelName { get { return Get<string>(); } set { SetLocalValue(value); } }

        public int Id { get { return Get<int>(); } set { SetLocalValue(value); } }

        public string LiveDevice { get { return Get<string>(); } set { SetLocalValue(value); } }

        public decimal MasterVolume { get { return Get<decimal>(); } set { SetLocalValue(value); } }

        public IPlayoutServer OwnerServer { get { return Get<IPlayoutServer>(); } set { SetLocalValue(value); } }

        public void Clear()
        {
            Invoke();
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            Invoke(parameters: new[] { aVideoLayer });
        }

        public void Initialize()
        {
            throw new NotImplementedException();
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

        public void SetVolume(VideoLayer videolayer, decimal volume)
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

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
