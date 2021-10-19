using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{
    internal class CgElementViewModel : ModifyableViewModelBase
    {
        private string _name = string.Empty;
        private string _command = string.Empty;
        private byte _id;
        private Bitmap _thumbnail;
        private BitmapImage _displayThumbnail;
        private readonly Model.CgElement _element;

        internal System.Windows.Window Window;

        public CgElementViewModel(Model.CgElement cgElement)
        {
            _element = cgElement;
            SelectThumbnailCommand = new UiCommand(SelectThumbnail);
            ClearThumbnailCommand = new UiCommand(ClearThumbnail, o => !(Thumbnail is null));
            Load();
        }

        public byte Id { get => _id; set => SetField(ref _id, value); }

        public string Name { get => _name; set => SetField(ref _name, value); }

        public string Command { get => _command; set => SetField(ref _command, value); }

        public Bitmap Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (!SetField(ref _thumbnail, value))
                    return;
                DisplayThumbnail = BitmapTools.BitmapToImageSource(value);
            }
        }

        public Model.CgElement Element => _element;

        public BitmapImage DisplayThumbnail { get => _displayThumbnail; set => SetFieldNoModify(ref _displayThumbnail, value); }

        public ICommand SelectThumbnailCommand { get; }

        public ICommand ClearThumbnailCommand { get; }

        private void Load()
        {
            SetFieldNoModify(ref _name, _element.Name, nameof(Name));
            SetFieldNoModify(ref _command, _element.Command, nameof(Command));
            SetFieldNoModify(ref _id, _element.Id, nameof(Id));
            SetFieldNoModify(ref _thumbnail, _element.Thumbnail, nameof(Thumbnail));
            SetFieldNoModify(ref _displayThumbnail, BitmapTools.BitmapToImageSource(_thumbnail), nameof(DisplayThumbnail));
            IsModified = false;
        }

        public void Update()
        {
            _element.Id = _id;
            _element.Name = _name;
            _element.Command = _command;
            _element.Thumbnail = _thumbnail;
        }

        protected override void OnDispose() { }

        private void SelectThumbnail(object _)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select file for thumbnail",
                Filter = "Portable Network Graphics|*.png",
                CheckFileExists = true,
            };
            if (dlg.ShowDialog(Window) != true)
                return;
            Thumbnail = new Bitmap(dlg.FileName);
        }

        private void ClearThumbnail(object obj)
        {
            Thumbnail = null;
        }

    }
}
