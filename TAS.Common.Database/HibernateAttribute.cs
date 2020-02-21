using System;

namespace TAS.Common.Database
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class HibernateAttribute: Attribute
    {
        public HibernateAttribute(string propertyName = null)
        {
            PropertyName = propertyName;
        }
        public string PropertyName { get; set; }
    }
}
