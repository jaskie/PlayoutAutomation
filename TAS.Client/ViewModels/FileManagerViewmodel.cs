using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Common;

namespace TAS.Client.ViewModels
{
    public class FileManagerViewmodel : ViewmodelBase
    {
        private ObservableCollection<FileOperationViewmodel> _operationList;
        private readonly IFileManager _fileManager;
        public FileManagerView View { get; private set; }

        public FileManagerViewmodel(IFileManager fileManager)
        {
            _fileManager = fileManager;
            fileManager.OperationAdded += FileManager_OperationAdded ;
            fileManager.OperationCompleted += FileManager_OperationCompleted;
            _operationList = new ObservableCollection<FileOperationViewmodel>(from fo in fileManager.OperationQueue select new FileOperationViewmodel(fo));
            View = new FileManagerView() { DataContext = this };
        }

        private void FileManager_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (_clearFinished)
                {
                    foreach (FileOperationViewmodel vm in _operationList.ToList())
                        if (vm.IsFileOperation(e.Operation))
                        {
                            _operationList.Remove(vm);
                            vm.Dispose();
                        }
                }
            }), null);
        }

        private void FileManager_OperationAdded(object sender, FileOperationEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
               _operationList.Add(new FileOperationViewmodel(e.Operation)))
                , null);
        }

        public ObservableCollection<FileOperationViewmodel> OperationList { get { return _operationList; } }


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
            _fileManager.OperationAdded -= FileManager_OperationAdded;
            _fileManager.OperationCompleted -= FileManager_OperationCompleted;
        }

        internal IConvertOperation CreateConvertOperation(IMedia sourceMedia, IMedia destMedia, TVideoFormat outputFormat, decimal audioVolume, TFieldOrder sourceFieldOrderEnforceConversion, TAspectConversion aspectConversion)
        {
            IConvertOperation result = _fileManager.CreateConvertOperation();
            result.SourceMedia = sourceMedia;
            result.DestMedia = destMedia;
            result.OutputFormat = outputFormat;
            result.AudioVolume = audioVolume;
            result.SourceFieldOrderEnforceConversion = sourceFieldOrderEnforceConversion;
            result.AspectConversion = aspectConversion;
            return result;
        }


    }
}
