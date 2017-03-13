using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class RecorderViewmodel : ViewmodelBase
    {
        public RecorderViewmodel(IRecorder recorder)
        {

        }

        protected override void OnDispose()
        {
            
        }
    }
}
