using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Common;
using TAS.Server;

namespace TAS.Client.ViewModels
{
    public class ExportViewmodel : ViewmodelBase
    {
        public ObservableCollection<MediaExportViewmodel> Items { get; private set; }
        public ICommand CommandExport { get; private set; }
        readonly MediaManager _mediaManager;
        Views.ExportView _view;
        public ExportViewmodel(MediaManager mediaManager, IEnumerable<MediaExport> exportList)
        {
            Items = new ObservableCollection<MediaExportViewmodel>(exportList.Select(m => new MediaExportViewmodel(m)));
            Directories = mediaManager.IngestDirectories.Where(d => d.IsXDCAM).ToList();
            SelectedDirectory = Directories.FirstOrDefault();
            CommandExport = new UICommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            _mediaManager = mediaManager;
            this._view = new Views.ExportView() { DataContext = this, Owner = System.Windows.Application.Current.MainWindow, ShowInTaskbar=false };
            _view.ShowDialog();
        }

        public List<IngestDirectory> Directories { get; private set; }

        IngestDirectory _selectedDirectory;
        public IngestDirectory SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                if (SetField(ref _selectedDirectory, value, "SelectedDirectory"))
                {
                    NotifyPropertyChanged("CommandExport");
                }
            }
        }

        void _export (object o)
        {
            _checking = true;
            NotifyPropertyChanged("CommandExport");
            try
            {
                //TODO: check if exporting files fit in device free space
            }
            finally
            {
                _checking = false;
                NotifyPropertyChanged("CommandExport");
            }
            _mediaManager.Export(Items.Select(mevm => mevm.MediaExport), SelectedDirectory);
            _view.Close();
        }

        bool _checking;
        bool _canExport(object o)
        {
            return !_checking && Items.Count > 0 && SelectedDirectory != null;
        }

        protected override void OnDispose()
        {
            _view = null;
        }
    }
}
