using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface ITemplated
    {
        IDictionary<string, string> Fields { get; set; }
        TemplateMethod Method { get; set; }
        int TemplateLayer { get; set; }
    }
}
