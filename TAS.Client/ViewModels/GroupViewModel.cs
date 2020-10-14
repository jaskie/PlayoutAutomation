using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class GroupViewModel: EditViewModelBase<IGroup>
    {
        private string _name;

        public GroupViewModel(IGroup group): base(group)
        {
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        protected override void Update(object destObject = null)
        {
            base.Update(destObject);
            Model.Save();
        }
        
        public ICommand CommandSave => new UiCommand(Update, o => IsModified);

        public ICommand CommandUndo => new UiCommand(Load, o => IsModified);

        protected override void OnDispose()
        {
        }
    }
}
