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
        IRemoteClient _client;
        internal void SetClient(IRemoteClient client)
        {
            if (_client != null)
                return;
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
            {
                result = client.Get<T>(this, propertyName);
                _properties[propertyName] = result;
                return (T)result;
            }
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

        protected void EventAdd([CallerMemberName] string eventName = null)
        {
            var client = _client;
            if (client != null)
                client.EventAdd(this, eventName);
        }

        protected void EventRemove([CallerMemberName] string eventName = null)
        {
            var client = _client;
            if (client != null)
                client.EventRemove(this, eventName);
        }

        public virtual void OnMessage(object sender, WebSocketMessageEventArgs e)
        {
            if (e.Message.DtoGuid == GuidDto)
            {
                Debug.WriteLine("OnMessage received {0}:{1}", this, e.Message.MemberName);
                if (e.Message.MemberName == "PropertyChanged")
                {
                    PropertyChangedEventArgs ea = JsonConvert.DeserializeObject<PropertyChangedEventArgs>(e.Message.Response.ToString());
                    NotifyPropertyChanged(ea.PropertyName);
                    object o;
                    _properties.TryRemove(ea.PropertyName, out o);
                }
            }
        }

        void NotifyPropertyChanged(string propertyName)
        {
            var h = _propertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(propertyName));
        }

        private ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        private event PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                var h = _propertyChanged;
                if (h == null || h.GetInvocationList().Length == 0)
                    EventAdd();
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
                var h = _propertyChanged;
                if (h == null || h.GetInvocationList().Length == 0)
                    EventRemove();
            }
        }


    }
}
