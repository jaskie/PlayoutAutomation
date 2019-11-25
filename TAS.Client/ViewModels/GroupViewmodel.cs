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
