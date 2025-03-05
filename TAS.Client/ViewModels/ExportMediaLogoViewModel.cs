using TAS.Client.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class ExportMediaLogoViewmodel: ViewModelBase
    {
        private readonly ExportMediaViewmodel _owner;
        public ExportMediaLogoViewmodel(ExportMediaViewmodel owner, IMedia logo)
        {
            Logo = logo;
            _owner = owner;
            CommandRemove = new UiCommand(CommandName(nameof(Remove)), Remove);
        }

        public IMedia Logo { get; }

        public UiCommand CommandRemove { get; }

        public override string ToString()
        {
            return Logo.MediaName;
        }

        protected override void OnDispose() { }

        private void Remove(object _)
        {
            _owner.Remove(this);
        }

    }
}
