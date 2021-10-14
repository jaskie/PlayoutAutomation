using System;
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
        private readonly Model.CgElement _element;

        public CgElementViewModel(Model.CgElement cgElement)
        {
            _element = cgElement;
            SelectThumbnailCommand = new UiCommand(SelectThumbnail);
            ClearThumbnailCommand = new UiCommand(ClearThumbnail, o => !(DisplayThumbnail is null));
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
            _name = _element.Name;
            _command = _element.Command;
            _id = _element.Id;
            _thumbnail = _element.Thumbnail;
            IsModified = false;
        }

        public void Update()
        {
            _element.Id = _id;
            _element.Name = _name;
            _element.Command = _command;
        }

        protected override void OnDispose() { }


        private void SelectThumbnail(object obj)
        {

        }

        private void ClearThumbnail(object obj)
        {

        }

    }
}
