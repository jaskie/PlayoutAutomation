using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaLogoViewmodel
    {
        private readonly ExportMediaViewmodel _owner;
        public ExportMediaLogoViewmodel(ExportMediaViewmodel owner, IMedia logo )
        {
            Logo = logo;
            _owner = owner;
            CommandRemove = new UICommand() { ExecuteDelegate = _remove };
        }

        public IMedia Logo { get; }

        public UICommand CommandRemove { get; }
        
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
