using System;

namespace TAS.Client.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnUpdateAttribute: Attribute
    {
    }
}
