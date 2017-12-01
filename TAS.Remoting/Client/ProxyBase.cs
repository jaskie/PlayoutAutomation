//#undef DEBUG

using Newtonsoft.Json;
using System;
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
            if (string.IsNullOrEmpty(propertyName))
                return default(T);
            var result = _client.Get<T>(this, propertyName);
            Debug.WriteLine($"Get:{result} for property {propertyName} of {this}");
            return result;
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
            _client.Set(this, value, propertyName);
        }

        protected void Invoke([CallerMemberName] string methodName = null, params object[] parameters)
        {
            _client.Invoke(this, methodName, parameters);
        }

        protected T Query<T>([CallerMemberName] string methodName = "", params object[] parameters)
        {
            return _client.Query<T>(this, methodName, parameters);
        }

        protected void EventAdd<T>(T handler, [CallerMemberName] string eventName = null)
        {
            if (handler == null && !DtoGuid.Equals(Guid.Empty))
                _client.EventAdd(this, eventName);
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
            _client = (RemoteClient)context.Context;
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
                    Debug.WriteLine($"{this}: property notified {eav.PropertyName}, value {eav.Value}");
                    Type type = GetType();
                    FieldInfo field = GetField(type, eav.PropertyName);
                    if (field == null)
                    {
                        var property = type.GetProperty(eav.PropertyName);
                        if (property != null)
                        {
                            var value = eav.Value;
                            MethodParametersAlignment.AlignType(ref value, property.PropertyType);
                            property.SetValue(this, value);
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
