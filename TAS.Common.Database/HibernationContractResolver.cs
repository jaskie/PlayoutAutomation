using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TAS.Common.Database
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
            var name = member.GetCustomAttribute<HibernateAttribute>()?.PropertyName;
            if (name != null)
                property.PropertyName = name;
            return property;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            contract.IsReference = false;
            return contract;
        }
    }
}
