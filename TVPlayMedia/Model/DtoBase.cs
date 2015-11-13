using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public abstract class DtoBase: IDto
    {
        public Guid GuidDto { get; set; }
        public abstract void OnMessage(object sender, WebSocketMessageEventArgs e);
        IRemoteClient _client;
        protected IRemoteClient Client { get { return _client; } }
        public void SetClient(IRemoteClient client)
        {
            client.OnMessage += OnMessage;
            _client = client;
        }
    }
}
