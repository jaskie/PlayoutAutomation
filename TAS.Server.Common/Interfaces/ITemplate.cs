using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ITemplate: INotifyPropertyChanged
    {
        string TemplateName { get; set; }
        int Layer { get; set; }
        Guid MediaGuid { get; }
        IMedia MediaFile { get; set; }
        Dictionary<string, string> TemplateFields { get; set; }

        void Save();
        void Delete();
    }
}
