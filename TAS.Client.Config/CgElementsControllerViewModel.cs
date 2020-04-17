using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class CgElementsControllerViewModel : EditViewmodelBase<CgElementsController>
    {
        public CgElementsControllerViewModel(CgElementsController cgElementsController) : base(cgElementsController)
        {

        }

        protected override void OnDispose()
        {
            //
        }
    }
}
