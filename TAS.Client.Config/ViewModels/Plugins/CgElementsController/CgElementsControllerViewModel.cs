using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementsControllerViewModel : EditViewmodelBase<Model.CgElementsController>
    {
        private List<CgElement> _cgElements;
        private List<CgElement> _crawls = new List<CgElement>();
        private List<CgElement> _logos = new List<CgElement>();
        private List<CgElement> _auxes = new List<CgElement>();
        private List<CgElement> _parentals = new List<CgElement>();
        private CgElement _selectedElement;
        private CgElement.Type _selectedElementType;
        private OkCancelViewModel _currentViewModel;
        public CgElementsControllerViewModel(Model.CgElementsController cgElementsController) : base(cgElementsController)
        {
            AddCgElementCommand = new UiCommand(AddCgElement);
            if (cgElementsController != null)
            {
                _crawls = new List<CgElement>(Model.Crawls);
                _logos = new List<CgElement>(Model.Logos);
                _auxes = new List<CgElement>(Model.Auxes); 
                _parentals = new List<CgElement>(Model.Parentals);
            }
            
            CgElements = CollectionViewSource.GetDefaultView(_cgElements);
        }      

        private void AddCgElement(object obj)
        {
            var element = new CgElement();
            _cgElements.Add(element);

            CurrentViewModel = new OkCancelViewModel(new CgElementViewModel(element, _selectedElementType == CgElement.Type.Parental ? true : false));                                                               
        }

        protected override void OnDispose()
        {
            //
        }

        public ICollectionView CgElements { get; }
        public OkCancelViewModel CurrentViewModel { get => _currentViewModel; set => SetField(ref _currentViewModel, value); }

        public string SelectedElementType
        {
            get => Enum.GetName(typeof(CgElement.Type), _selectedElementType);
            set
            {               
                var temp = (CgElement.Type)Enum.Parse(typeof(CgElement.Type), value);
                if (temp == _selectedElementType)
                    return;

                _selectedElementType = temp;
                switch(_selectedElementType)
                {
                    case CgElement.Type.Crawl:
                        _cgElements = _crawls;
                        CgElements.Refresh();
                        break;
                    case CgElement.Type.Logo:
                        _cgElements = _logos;
                        CgElements.Refresh();
                        break;
                    case CgElement.Type.Aux:
                        _cgElements = _auxes;
                        CgElements.Refresh();
                        break;
                    case CgElement.Type.Parental:
                        _cgElements = _parentals;
                        CgElements.Refresh();
                        break;
                }    
                NotifyPropertyChanged(nameof(SelectedElementType));                
            }
        }
        public CgElement SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (!SetField(ref _selectedElement, value))
                    return;

                CurrentViewModel = new OkCancelViewModel(new CgElementViewModel(value, _selectedElementType == CgElement.Type.Parental ? true : false));
                    
            }
        }

        public UiCommand AddCgElementCommand { get; private set; }
    }
}
