using Newtonsoft.Json;
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

            //if (member.PropertyType.IsInterface)
            //{
            //    IEnumerable<Type> modelTypes = null;

            //    if (objectType.GetCustomAttribute<DataModel>(false).ModelType == DataModel.DataType.Configuration)
            //    {
            //        modelTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<DataModel>(false)?.ModelType == DataModel.DataType.Configuration && objectType.IsAssignableFrom(t));
            //    }
            //    else
            //    {
            //        modelTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<DataModel>(false)?.ModelType == DataModel.DataType.Main && objectType.IsAssignableFrom(t));
            //    }
            //    contract = base.CreateObjectContract(modelTypes.FirstOrDefault());
            //}
            //else
            //{
            //    contract = base.CreateObjectContract(objectType);
            //}

            var name = member.GetCustomAttribute<HibernateAttribute>()?.PropertyName;
            if (name != null)
                property.PropertyName = name;
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
