using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface ITemplated
    {
        IDictionary<string, string> Fields { get; set; }
        TemplateMethod Method { get; set; }
        int TemplateLayer { get; set; }
    }
}
