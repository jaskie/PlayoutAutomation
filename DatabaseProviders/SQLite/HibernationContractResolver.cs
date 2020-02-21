using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TAS.Common.Database;

namespace TAS.Database.SQLite
{
    internal class HibernationContractResolver: DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            return objectType.GetProperties().Where(p => p.GetCustomAttribute(typeof(HibernateAttribute)) != null).Cast<MemberInfo>().ToList();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.Ignored = false;
            var name = member.GetCustomAttribute<HibernateAttribute>()?.PropertyName;
            if (name != null)
                property.PropertyName = name;
            return property;
        }
    }
}
