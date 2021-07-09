using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{
    public class CgElementViewModel : ModifyableViewModelBase
    {
        private string _name = string.Empty;
        private string _command = string.Empty;
        private byte _id;
        private Bitmap _thumbnail;
        private BitmapImage _displayThumbnail;
        private readonly Model.CgElement _cgElement;

        public CgElementViewModel(Model.CgElement cgElement)
        {
            _cgElement = cgElement;
            SelectThumbnailCommand = new UiCommand(SelectThumbnail);
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

        public BitmapImage DisplayThumbnail { get => _displayThumbnail; set => SetFieldNoModify(ref _displayThumbnail, value); }

        public ICommand SelectThumbnailCommand { get; }

        private void Load()
        {
            _name = _cgElement.Name;
            _command = _cgElement.Command;
            _id = _cgElement.Id;
            _thumbnail = _cgElement.Thumbnail;
            IsModified = false;
        }

        public void Update()
        {
            _cgElement.Id = _id;
            _cgElement.Name = _name;
            _cgElement.Command = _command;
        }

        protected override void OnDispose()
        {
            //
        }


        private void SelectThumbnail(object obj)
        {

        }

    }
}
