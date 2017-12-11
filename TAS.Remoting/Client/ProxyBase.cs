#undef DEBUG

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace TAS.Remoting.Client
{
    public abstract class ProxyBase : IDto, INotifyPropertyChanged
    {

#if DEBUG
        ~ProxyBase()
        {
            Debug.WriteLine(this, string.Format("{0} Finalized", GetType().FullName));
        }
#endif

        public Guid DtoGuid { get; set; }
        private RemoteClient _client;

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            object result;
            _findPropertyName(ref propertyName);
            if (Properties.TryGetValue(propertyName, out result))
                return  (T)result;
            var client = _client;
            if (client != null)
            {
                result = client.Get<T>(this, propertyName);
                Properties[propertyName] = result;
                return (T)result;
            }
            return default(T);
        }

        private void _findPropertyName(ref string propertyName)
        {
            var property = this.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
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
            if (SetLocalValue(value, propertyName))
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

        protected bool SetLocalValue(object value, [CallerMemberName] string propertyName = null)
        {
            object oldValue;
            _findPropertyName(ref propertyName);
            if (!Properties.TryGetValue(propertyName, out oldValue)  // here values may be boxed
                || (oldValue != value && (oldValue != null && !oldValue.Equals(value)) || (value != null && !value.Equals(oldValue))))
            {
                Properties[propertyName] = value;
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
                    PropertyChangedWithValueEventArgs eav = e.Message.Response as PropertyChangedWithValueEventArgs;
                    if (eav != null)
                    {
                        Type type = this.GetType();
                        PropertyInfo property =
                            type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(p => p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).Where(a => ((JsonPropertyAttribute)a).PropertyName == eav.PropertyName).Any());
                        if (property != null)
                            Debug.WriteLine(property.Name);
                        if (property == null)
                            property = type.GetProperty(eav.PropertyName);
//                        if (property.Name == "Commands")
//                            Debug.WriteLine(property.Name);
                        object value = eav.Value;
                        if (property != null)
                            MethodParametersAlignment.AlignType(ref value, property.PropertyType);
                        Properties[eav.PropertyName] = value;
                        NotifyPropertyChanged(eav.PropertyName);
                    }
                }
                else OnEventNotification(e.Message);
            }
        }

        protected abstract void OnEventNotification(WebSocketMessage e);

        protected virtual void OnEventRegistration(WebSocketMessage e) { }

        protected T ConvertEventArgs<T>(WebSocketMessage e) where T : EventArgs
        {
            return (T)e.Response;
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected ConcurrentDictionary<string, object> Properties = new ConcurrentDictionary<string, object>();

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isDisposed = false;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                DoDispose();
            }
        }

        protected virtual void DoDispose()
        {
            _client.EventNotification -= _onEventNotificationMessage;
            _client = null;
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Disposed;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            _client = context.Context as RemoteClient;
            _client.EventNotification += _onEventNotificationMessage;
        }

    }
}
