using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface ITemplated: INotifyPropertyChanged
    {
        Dictionary<string, string> Fields { get; set; }
        TemplateMethod Method { get; set; }
        int TemplateLayer { get; set; }
        TimeSpan ScheduledDelay { get; set; }
        TStartType StartType { get; set; }
    }
}
