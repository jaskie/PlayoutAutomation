using System;
using TAS.Common;

namespace TAS.Database.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class HibernateAttribute: Attribute
    {       
        public HibernateAttribute(string propertyName = null, DataType modelType = DataType.Main)
        {
            ModelType = modelType;
            PropertyName = propertyName;
        }
        public string PropertyName { get; set; }
        public DataType ModelType { get; }
    }
}
