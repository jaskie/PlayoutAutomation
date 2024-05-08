using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class GroupViewmodel: EditViewmodelBase<IGroup>
    {
        private string _name;

        public GroupViewmodel(IGroup group): base(group)
        {
            CommandSave = new UiCommand(CommandName(nameof(Update)), Update, _ => IsModified);
            CommandUndo = new UiCommand(nameof(Load), Load, _ => IsModified);
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

        public ICommand CommandSave { get; }

        public ICommand CommandUndo { get; }

        protected override void OnDispose() { }
    }
}
