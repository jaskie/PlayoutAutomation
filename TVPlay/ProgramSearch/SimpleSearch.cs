using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TAS.Client.ProgramSearch
{
    class SimpleSearch : IProgramSearch
    {
        public ObservableCollection<Program> Search(string Title)
        {
            return new ObservableCollection<Program> 
            {
                new Program() { idProgram = 0, Title=Title+"1", Duration = TimeSpan.Zero },
                new Program() { idProgram = 1, Title=Title+"2", Duration = TimeSpan.FromMinutes(3.5) }
            };
        }
    }
}
