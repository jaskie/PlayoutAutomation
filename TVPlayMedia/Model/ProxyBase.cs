using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public abstract class ProxyBase: IDto
    {
        public Guid GuidDto { get; set; }
        public abstract void OnMessage(object sender, WebSocketMessageEventArgs e);
        IRemoteClient _client;
        public void SetClient(IRemoteClient client)
        {
            client.OnMessage += OnMessage;
            _client = client;
            Debug.WriteLine(GuidDto, "Client assigned");
        }
        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            var client = _client;
            if (client != null)
                return client.Get<T>(this, propertyName);
            return default(T);
        }
        protected void Invoke([CallerMemberName] string methodName = null)
        {
            var client = _client;
            if (client != null)
                client.Invoke(this, methodName);
        }
        protected T Query<T> ([CallerMemberName] string methodName = "", params object[] parameters)
        {
            var client = _client;
            if (client != null)
                return client.Query<T>(this, methodName, parameters);
            return default(T);
        }

    }
}
