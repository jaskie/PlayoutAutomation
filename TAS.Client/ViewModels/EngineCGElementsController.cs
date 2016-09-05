using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EngineCGElementsController : ViewmodelBase
    {
        public readonly ICGElementsController Controller;
        public EngineCGElementsController(ICGElementsController controller)
        {
            Controller = controller;
        }

        protected override void OnDispose() { }
    }
}
