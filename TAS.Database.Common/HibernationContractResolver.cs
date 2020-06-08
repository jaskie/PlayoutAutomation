﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
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
                property.TypeNameHandling = TypeNameHandling.Objects;                
                       

            if (propertyAttribute?.PropertyName != null)
                property.PropertyName = propertyAttribute.PropertyName;

            return property;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);
            contract.IsReference = false;            
            return contract;
        }
    }
}