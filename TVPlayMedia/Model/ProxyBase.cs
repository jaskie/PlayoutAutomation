using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public abstract class ProxyBase: IDto, INotifyPropertyChanged
    {
        [JsonProperty]
        public Guid GuidDto { get; set; }
        public abstract void OnMessage(object sender, WebSocketMessageEventArgs e);
        IRemoteClient _client;
        internal void SetClient(IRemoteClient client)
        {
            client.OnMessage += OnMessage;
            _client = client;
            Debug.WriteLine(this, "Client assigned");
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            object result;
            if (_properties.TryGetValue(propertyName, out result))
                return (T)result;

            var client = _client;
            if (client != null)
                return client.Get<T>(this, propertyName);
            return default(T);
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var client = _client;
            if (client != null)
                client.Set(this, value, propertyName);
            _properties[propertyName] = value;
        }

        protected void Invoke([CallerMemberName] string methodName = null, params object[] parameters)
        {
            var client = _client;
            if (client != null)
                client.Invoke(this, methodName, parameters);
        }
        protected T Query<T> ([CallerMemberName] string methodName = "", params object[] parameters)
        {
            var client = _client;
            if (client != null)
                return client.Query<T>(this, methodName, parameters);
            return default(T);
        }

        private ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        private event PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged { add { _propertyChanged += value; } remove { _propertyChanged -= value; } }


    }
}
