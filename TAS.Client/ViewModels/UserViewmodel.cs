using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserViewmodel: EditViewmodelBase<IUser>
    {
        private string _name;

        public UserViewmodel(IUser user): base(user)
        {
        }

        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }

        public override void Update(object destObject = null)
        {
            base.Update(destObject);
            Model.Save();
        }

        
        public ICommand CommandSaveEdit => new UICommand {ExecuteDelegate = o => Update(), CanExecuteDelegate = o => IsModified};

        protected override void OnDispose()
        {
        }
    }
}
