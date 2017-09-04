#undef DEBUG

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace TAS.Remoting.Client
{
    [JsonObject(IsReference = true, MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ProxyBase : IDto
    {
        private int _isDisposed;
        private RemoteClient _client;

        // property cache
        private readonly ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == default(int))
                DoDispose();
        }


#if DEBUG
        ~ProxyBase()
        {
            Debug.WriteLine(this, string.Format("{0} Finalized", GetType().FullName));
        }
#endif

        public Guid DtoGuid { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler Disposed;

        protected T Get<T>([CallerMemberName] string propertyName = null)
        {
            object result;
            if (_properties.TryGetValue(propertyName, out result))
                if (typeof(T).IsEnum && result is long)
                {
                    int ev = (int) (long) result;
                    return (T) Enum.Parse(typeof(T), ev.ToString());
                }
                else
                    return (T) result;
            if (_client != null)
            {
                result = _client.Get<T>(this, propertyName);
                _properties[propertyName] = result;
                return (T)result;
            }
            return default(T);
        }

        protected void Set<T>(T value, [CallerMemberName] string propertyName = null)
        {
            Type type = GetType();
            FieldInfo field = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(p =>
                    p.GetCustomAttributes(typeof(JsonPropertyAttribute), true)
                        .Any(a => ((JsonPropertyAttribute) a).PropertyName == propertyName));
            if (field != null)
            {
                var currentValue = field.GetValue(this);
                if (value.Equals(currentValue))
                    return;
            }
            _client?.Set(this, value, propertyName);
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
                client?.EventAdd(this, eventName);
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

        protected abstract void OnEventNotification(WebSocketMessage message);

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void DoDispose()
        {
            _client = null;
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            var client = context.Context as RemoteClient;
            if (client == null)
                return;
            _client = client;
        }

        protected T Deserialize<T>(WebSocketMessage message)
        {
            if (_client == null)
                return default(T);
            return _client.Deserialize<T>(message);
        }

        internal void OnEventNotificationMessage(WebSocketMessage message)
        {
            if (message.MemberName == nameof(INotifyPropertyChanged.PropertyChanged))
            {
                PropertyChangedWithDataEventArgs eav = Deserialize<PropertyChangedWithDataEventArgs>(message);
                if (eav != null)
                {
                    Type type = GetType();
                    FieldInfo field = GetField(type, eav.PropertyName);
                    if (field == null)
                    {
                        var property = type.GetProperty(eav.PropertyName);
                        if (property != null)
                        {
                            var value = eav.Value;
                            MethodParametersAlignment.AlignType(ref value, property.PropertyType);
                            if (_properties.ContainsKey(eav.PropertyName))
                                _properties[eav.PropertyName] = value;
                        }
                    }
                    else
                    {
                        var value = eav.Value;
                        MethodParametersAlignment.AlignType(ref value, field.FieldType);
                        field.SetValue(this, value);
                    }
                    NotifyPropertyChanged(eav.PropertyName);
                }
            }
            else OnEventNotification(message);
        }

        protected FieldInfo GetField(Type t, string fieldName)
        {
            if (t == null)
                return null;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            FieldInfo foundField = t.GetFields(flags).FirstOrDefault(f => f.GetCustomAttributes(typeof(JsonPropertyAttribute), true).Any(a =>((JsonPropertyAttribute)a).PropertyName == fieldName));
            return foundField ?? GetField(t.BaseType, fieldName);
        }
    }
}
