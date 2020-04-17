using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementsControllerViewModel : EditViewmodelBase<Model.CgElementsController>
    {
        public CgElementsControllerViewModel(Model.CgElementsController cgElementsController) : base(cgElementsController)
        {

        }

        protected override void OnDispose()
        {
            //
        }
    }
}
