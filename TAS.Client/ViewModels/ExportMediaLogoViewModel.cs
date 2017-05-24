using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportMediaLogoViewmodel
    {
        readonly ExportMediaViewmodel Owner;
        public readonly IMedia Logo;
        public ExportMediaLogoViewmodel(ExportMediaViewmodel owner, IMedia logo )
        {
            Logo = logo;
            Owner = owner;
            CommandRemove = new UICommand() { ExecuteDelegate = _remove };
        }

        private void _remove(object obj)
        {
            Owner.Remove(this);
        }

        public UICommand CommandRemove { get; private set; }


        public override string ToString()
        {
            return Logo.MediaName;
        }
    }
}
