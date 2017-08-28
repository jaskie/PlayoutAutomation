using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public abstract class AclRightViewmodel: ViewmodelBase
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
