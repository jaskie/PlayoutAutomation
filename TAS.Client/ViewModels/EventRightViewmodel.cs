using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Client.ViewModels
{
    public class EventRightViewmodel: AclRightViewmodel
    {
        private bool _create;
        private bool _delete;
        private bool _modify;

        public EventRightViewmodel(IAclRight right) : base(right)
        {
            Acl = right.Acl;
            Load();
        }

        public bool Create
        {
            get => _create;
            set
            {
                if (!SetField(ref _create, value))
                    return;
                if (value)
                    Acl |= (ulong)EventRight.Create;
                else
                    Acl &= ~(ulong)EventRight.Create;
            }
        }

        public bool Delete
        {
            get => _delete;
            set
            {
                if (!SetField(ref _delete, value))
                    return;
                if (value)
                    Acl |= (ulong)EventRight.Delete;
                else
                    Acl &= ~(ulong)EventRight.Delete;
            }
        }

        public bool Modify
        {
            get => _modify;
            set
            {
                if (!SetField(ref _modify, value))
                    return;
                if (value)
                    Acl |= (ulong)EventRight.Modify;
                else
                    Acl &= ~(ulong)EventRight.Modify;
            }
        }

        public ISecurityObject SecurityObject => Right.SecurityObject;

        public void Load()
        {
            _create = (Acl & (ulong)EventRight.Create) != 0;
            _modify = (Acl & (ulong)EventRight.Modify) != 0;
            _delete = (Acl & (ulong)EventRight.Delete) != 0;
        }
    }
}
