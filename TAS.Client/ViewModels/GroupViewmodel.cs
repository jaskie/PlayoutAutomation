using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class GroupViewmodel: EditViewmodelBase<IGroup>
    {
        private string _name;

        public GroupViewmodel(IGroup group): base(group)
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
        
        public ICommand CommandSave => new UICommand {ExecuteDelegate = Update, CanExecuteDelegate = o => IsModified};

        public ICommand CommandUndo => new UICommand { ExecuteDelegate = Load, CanExecuteDelegate = o => IsModified };

        protected override void OnDispose()
        {
        }
    }
}
