#undef DEBUG

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using WebSocketSharp;

namespace TAS.Remoting.Client
{
    //[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ProxyBase : IDto, INotifyPropertyChanged
    {
        public Guid DtoGuid { get; set; }
        private RemoteClient _client;
        private void SetClient(RemoteClient client)
        {
            if (_client != null)
                return;
            client.EventNotification += _onEventNotificationMessage;
            _client = client;
        }

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            object result;
            _findPropertyName(ref propertyName);
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

        private void _findPropertyName(ref string propertyName)
        {
            var property = this.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (property != null)
            {
                var attributes = property.GetCustomAttributes(typeof(JsonPropertyAttribute), true);
                foreach (JsonPropertyAttribute attr in attributes)
                    if (!string.IsNullOrWhiteSpace(attr.PropertyName))
                    {
                        propertyName = attr.PropertyName;
                        return;
                    }
            }
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            _findPropertyName(ref propertyName);
            var client = _client;
            if (SetField(value, propertyName))
            {
                if (client != null)
                    client.Set(this, value, propertyName);
            }
        }

        protected void Invoke([CallerMemberName] string methodName = null, params object[] parameters)
        {
            var client = _client;
            if (client != null)
                client.Invoke(this, methodName, parameters);
        }
        protected T Query<T>([CallerMemberName] string methodName = "", params object[] parameters)
        {
            var client = _client;
            if (client != null)
                return client.Query<T>(this, methodName, parameters);
            return default(T);
        }

        protected void EventAdd<T>(T handler, [CallerMemberName] string eventName = null)
        {
            if (handler == null && !DtoGuid.Equals(Guid.Empty))
            {
                var client = _client;
                if (client != null)
                {
                    client.EventAdd(this, eventName);
                }
            }
        }

        protected void EventRemove<T>(T handler, [CallerMemberName] string eventName = null)
        {
            if (handler == null && !DtoGuid.Equals(Guid.Empty))
            {
                var client = _client;
                if (client != null)
                    client.EventRemove(this, eventName);
            }
        }

        protected bool SetField(object value, [CallerMemberName] string propertyName = null)
        {
            object oldValue;
            if (!_properties.TryGetValue(propertyName, out oldValue)  // here values may be boxed
                || (oldValue != value && (oldValue != null && !oldValue.Equals(value)) || (value != null && !value.Equals(oldValue))))
            {
                _properties[propertyName] = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        void _onEventNotificationMessage(object sender, WebSocketMessageEventArgs e)
        {
            if (e.Message.DtoGuid == DtoGuid)
            {
                Debug.WriteLine($"ProxyBase: Event {e.Message.MemberName} notified on {this} with value {e.Message.Response}");
                if (e.Message.MemberName == nameof(INotifyPropertyChanged.PropertyChanged))
                {
                    PropertyChangedEventArgs ea = (PropertyChangedEventArgs)e.Message.Response;
                    NotifyPropertyChanged(ea.PropertyName);
                    object o;
                    _properties.TryRemove(ea.PropertyName, out o);
                }
                else OnEventNotification(e);
            }
        }

        protected virtual void OnEventNotification(WebSocketMessageEventArgs e)
        {
            Debug.WriteLine(this, e.ToString());
        }

        protected virtual void OnEventRegistration(WebSocketMessageEventArgs e) { }

        protected T ConvertEventArgs<T>(WebSocketMessageEventArgs e) where T : EventArgs
        {
            return (T)e.Message.Response;
        }

        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isDisposed = false;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _client.EventNotification -= _onEventNotificationMessage;
                _isDisposed = true;
                DoDispose();
            }
        }

        protected virtual void DoDispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            SetClient((RemoteClient)context.Context);
        }

    }
}
