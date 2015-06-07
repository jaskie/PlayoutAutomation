using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TAS.Client.ProgramSearch
{
    public class Program
    {
        public UInt64 idProgram { get; set; }
        public string Identifier { get; set; }
        public string Title { get; set; }
        public TimeSpan Duration {get; set;}
    }

    interface IProgramSearch
    {
        ObservableCollection<Program> Search(string Title);
    }
}
