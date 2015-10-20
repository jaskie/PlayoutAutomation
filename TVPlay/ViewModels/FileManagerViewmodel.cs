using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace TAS.Client.ViewModels
{
    class FileManagerViewmodel : ViewmodelBase
    {
        private ObservableCollection<FileOperationViewmodel> _operationList;
        public FileManagerViewmodel()
        {
            FileManager.OperationAdded += OnOperationAdded;
            FileManager.OperationCompleted += OnOperationCompleted;
            _operationList = new ObservableCollection<FileOperationViewmodel>(from fo in FileManager.OperationQueue select new FileOperationViewmodel(fo));
        }

        public ObservableCollection<FileOperationViewmodel> OperationList { get { return _operationList; } }

        private void OnOperationAdded(FileOperation operation)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)( ()=>
                _operationList.Add(new FileOperationViewmodel(operation)))
                , null);
        }

        private void OnOperationCompleted(FileOperation operation)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_clearFinished)
                {
                    foreach (FileOperationViewmodel vm in _operationList.ToList())
                        if (vm.IsFileOperation(operation))
                        {
                            _operationList.Remove(vm);
                            vm.Dispose();
                        }
                }
            }), null);
        }

        private bool _clearFinished;
        public bool ClearFinished
        {
            get { return _clearFinished; }
            set
            {
                if (_clearFinished != value)
                {
                    _clearFinished = value;
                    foreach (FileOperationViewmodel vm in _operationList.ToList())
                        if (vm.Finished)
                        {
                            _operationList.Remove(vm);
                            vm.Dispose();
                        }
                    NotifyPropertyChanged("ClearFinished");
                }
            }
        }

        protected override void OnDispose()
        {
            FileManager.OperationAdded -= OnOperationAdded;
            FileManager.OperationCompleted -= OnOperationCompleted;
        }
    }
}
