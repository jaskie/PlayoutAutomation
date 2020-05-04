using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementsControllerViewModel : EditViewmodelBase<Model.CgElementsController>
    {
        private List<CgElement> _cgElements;
        public CgElementsControllerViewModel(Model.CgElementsController cgElementsController) : base(cgElementsController)
        {
            CgElements = CollectionViewSource.GetDefaultView(_cgElements);
        }

        protected override void OnDispose()
        {
            //
        }

        public ICollectionView CgElements { get; }
    }
}
