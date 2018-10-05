using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public abstract class AclRightViewmodel: ModifyableViewModelBase
    {
        protected AclRightViewmodel(IAclRight right)
        {
            Right = right;
        }

        public IAclRight Right { get; }

        protected ulong Acl;

        public virtual void Save()
        {
            Right.Acl = Acl;
            (Right as IPersistent)?.Save();
        }

        protected override void OnDispose()
        {

        }

    }
}
