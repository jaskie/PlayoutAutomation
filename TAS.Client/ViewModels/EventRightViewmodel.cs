using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

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
            _create = (right.Acl & (ulong)EventRight.Create) != 0;
            _modify = (right.Acl & (ulong)EventRight.Modify) != 0;
            _delete = (right.Acl & (ulong)EventRight.Delete) != 0;
        }

        public bool Create
        {
            get { return _create; }
            set
            {
                if (SetField(ref _create, value))
                    if (value)
                        Acl |= (ulong)EventRight.Create;
                    else
                        Acl &= ~(ulong)EventRight.Create;
            }
        }

        public bool Delete
        {
            get { return _delete; }
            set
            {
                if (SetField(ref _delete, value))
                    if (value)
                        Acl |= (ulong)EventRight.Delete;
                    else
                        Acl &= ~(ulong)EventRight.Delete;
            }
        }

        public bool Modify
        {
            get { return _modify; }
            set
            {
                if (SetField(ref _modify, value))
                    if (value)
                        Acl |= (ulong)EventRight.Modify;
                    else
                        Acl &= ~(ulong)EventRight.Modify;

            }
        }

        public ISecurityObject SecurityObject => Right.SecurityObject;
    }
}
