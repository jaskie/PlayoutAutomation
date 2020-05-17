using System;

namespace TAS.Client.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataContextAttribute : Attribute
    {
        public string ViewModelName { get; }
        public DataContextAttribute(string viewModelName)
        {
            ViewModelName = viewModelName;
        }
    }
}
