using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TAS.Server.Advantech.Configurator.Model;

namespace TAS.Server.AdvantechTests
{
    [TestClass]
    public class AdvantechTestData
    {
        public static List<Gpi> Gpis = new List<Gpi>
        {
            null,
            new Gpi
            {                
                Bindings = new ObservableCollection<GpiBinding>
                {
                    new GpiBinding(1,1,1),
                    new GpiBinding(1,2,1),
                    new GpiBinding(2,2,2),
                    new GpiBinding(3,3,3),
                },
                IsEnabled = false
            }
        };
    }
}
