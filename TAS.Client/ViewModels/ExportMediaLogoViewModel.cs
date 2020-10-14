using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class ExportMediaLogoViewModel
    {
        private readonly ExportMediaViewModel _owner;
        public ExportMediaLogoViewModel(ExportMediaViewModel owner, IMedia logo )
        {
            Logo = logo;
            _owner = owner;
            CommandRemove = new UiCommand(_remove);
        }

        public IMedia Logo { get; }

        public UiCommand CommandRemove { get; }
        
        public override string ToString()
        {
            return Logo.MediaName;
        }


        private void _remove(object obj)
        {
            _owner.Remove(this);
        }

    }
}
