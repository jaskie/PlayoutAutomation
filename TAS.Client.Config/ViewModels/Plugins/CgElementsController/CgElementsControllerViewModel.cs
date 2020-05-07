using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementsControllerViewModel : EditViewmodelBase<Model.CgElementsController>
    {
        private List<CgElement> _cgElements;
        private List<CgElement> _crawls;
        private List<CgElement> _logos;
        private List<CgElement> _auxes;
        private List<CgElement> _parentals;
        private CgElement _selectedElement;
        private CgElement.Type _selectedElementType;
        private OkCancelViewModel _currentViewModel;
        private List<string> _elementTypes;

        private CgElement _newElement;

        public CgElementsControllerViewModel(Model.CgElementsController cgElementsController) : base(cgElementsController)
        {
            LoadCommands();
            Init();
        }        

        private void Init()
        {
            _crawls = new List<CgElement>(Model.Crawls);
            _logos = new List<CgElement>(Model.Logos);
            _auxes = new List<CgElement>(Model.Auxes);
            _parentals = new List<CgElement>(Model.Parentals);
            _elementTypes = Enum.GetNames(typeof(CgElement.Type)).ToList();

            ElementTypes = CollectionViewSource.GetDefaultView(_elementTypes);
            SelectedElementType = _elementTypes.LastOrDefault();
        }

        private void LoadCommands()
        {
            AddCgElementCommand = new UiCommand(AddCgElement, CanAddCgElement);
            MoveCgElementUpCommand = new UiCommand(MoveCgElementUp, CanMoveCgElementUp);
            MoveCgElementDownCommand = new UiCommand(MoveCgElementDown, CanMoveCgElementDown);
            EditElementCommand = new UiCommand(EditElement);
            DeleteElementCommand = new UiCommand(DeleteElement);
            SaveCommand = new UiCommand(Save, CanSave);
            UndoCommand = new UiCommand(Undo, CanUndo);
        }

        private bool CanUndo(object obj)
        {
            return IsModified;
        }

        private void Undo(object obj)
        {
            _newElement = null;
            CurrentViewModel = null;
            base.Load();
            Init();            
        }

        private bool CanSave(object obj)
        {
            return IsModified;
        }

        private void Save(object obj)
        {
            base.Update();
        }

        private void DeleteElement(object obj)
        {
            if (!(obj is CgElement element))
                return;

            _cgElements.Remove(element);
            CgElements.Refresh();
        }

        private void EditElement(object obj)
        {
            if (!(obj is CgElement element))
                return;

            CurrentViewModel = new OkCancelViewModel(new CgElementViewModel(element, _selectedElementType == CgElement.Type.Parental ? true : false), "Edit");
        }

        private bool CanMoveCgElementDown(object obj)
        {
            if (_selectedElement != null && _cgElements.LastOrDefault() != _selectedElement)
                return true;
            
            return false;
        }

        private void MoveCgElementDown(object obj)
        {
            var index = _cgElements.IndexOf(_selectedElement);
            var temp = _cgElements[index];

            _cgElements[index] = _cgElements[index + 1];
            _cgElements[index + 1] = temp;
            CgElements.Refresh();
        }

        private bool CanMoveCgElementUp(object obj)
        {
            if (_selectedElement != null && _cgElements.FirstOrDefault() != _selectedElement)
                return true;

            return false;
        }

        private void MoveCgElementUp(object obj)
        {
            var index = _cgElements.IndexOf(_selectedElement);
            var temp = _cgElements[index];

            _cgElements[index] = _cgElements[index - 1];
            _cgElements[index - 1] = temp;
            CgElements.Refresh();
        }

        private bool CanAddCgElement(object obj)
        {
            if (_newElement == null)
                return true;

            return false;
        }

        private void AddCgElement(object obj)
        {
            _newElement = new CgElement();            
            CurrentViewModel = new OkCancelViewModel(new CgElementViewModel(_newElement, _selectedElementType == CgElement.Type.Parental ? true : false), "Add");                                                               
        }

        private void OkCancelClosed(object sender, EventArgs e)
        {
            if (!(sender is OkCancelViewModel okCancelVm))
                return;

            if (okCancelVm.DialogResult)
            {
                if (_newElement != null)
                {
                    _cgElements.Add(_newElement);                    
                    _newElement = null;
                }
                
                Update();
            }
            CurrentViewModel = null;
            CgElements.Refresh();
        }

        protected override void OnDispose()
        {
            //
        }

        public ICollectionView CgElements { get; private set; }
        public ICollectionView ElementTypes { get; private set; }
        public OkCancelViewModel CurrentViewModel 
        { 
            get => _currentViewModel;
            set
            {
                var old = _currentViewModel;                

                if (!SetField(ref _currentViewModel, value))
                    return;                

                if (old != null)                
                    old.Closing -= OkCancelClosed;
                
                if (value != null)
                    _currentViewModel.Closing += OkCancelClosed;
            }
        }        

        public string SelectedElementType
        {
            get => Enum.GetName(typeof(CgElement.Type), _selectedElementType);
            set
            {               
                var temp = (CgElement.Type)Enum.Parse(typeof(CgElement.Type), value);
                if (temp == _selectedElementType)
                    return;

                _newElement = null;
                CurrentViewModel = null;

                _selectedElementType = temp;
                switch(_selectedElementType)
                {
                    case CgElement.Type.Crawl:
                        _cgElements = _crawls;                       
                        break;
                    case CgElement.Type.Logo:
                        _cgElements = _logos;                       
                        break;
                    case CgElement.Type.Aux:
                        _cgElements = _auxes;                       
                        break;
                    case CgElement.Type.Parental:
                        _cgElements = _parentals;                        
                        break;
                }

                CgElements = CollectionViewSource.GetDefaultView(_cgElements);              

                NotifyPropertyChanged(nameof(CgElements));
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

                _newElement = null;                                                    
            }
        }

        public UiCommand AddCgElementCommand { get; private set; }
        public UiCommand MoveCgElementUpCommand { get; private set; }
        public UiCommand MoveCgElementDownCommand { get; private set; }
        public UiCommand EditElementCommand { get; private set; }
        public UiCommand DeleteElementCommand { get; private set; }
        public UiCommand SaveCommand { get; private set; }
        public UiCommand UndoCommand { get; private set; }
    }
}
