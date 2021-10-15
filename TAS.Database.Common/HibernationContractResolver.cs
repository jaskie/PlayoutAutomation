using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TAS.Database.Common
{
    public class HibernationContractResolver: DefaultContractResolver
    {               
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return objectType.GetMembers().Where(p => p.GetCustomAttribute<HibernateAttribute>() != null).ToList();
        }
        
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.Ignored = false;
            
            var propertyAttribute = member.GetCustomAttribute<HibernateAttribute>();

            if (property.PropertyType.IsInterface)
            {
                property.TypeNameHandling = TypeNameHandling.Objects;                
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    property.ItemTypeNameHandling = TypeNameHandling.Objects;
            }
                
            else if (property.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))                            
                property.ItemTypeNameHandling = TypeNameHandling.Objects;                        

            if (propertyAttribute?.PropertyName != null)
                property.PropertyName = propertyAttribute.PropertyName;

            return property;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);
            contract.IsReference = false;
            //if (typeof(IPlugin).IsAssignableFrom(objectType))
            //    contract.Converter = PluginConverter.Current;
            return contract;
        }

        protected override JsonStringContract CreateStringContract(Type objectType)
        {
            var contract = base.CreateStringContract(objectType);
            if (typeof(System.Drawing.Bitmap).IsAssignableFrom(objectType))
                contract.Converter = BitmapJsonConverter.Current;
            return contract;
        }
    }
}
