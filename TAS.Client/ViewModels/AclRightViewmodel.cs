using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class AclRightViewmodel: ViewmodelBase
    {
        public AclRightViewmodel(IAclRight right)
        {
            Right = right;
        }

        public IAclRight Right { get; }

        public void Save()
        {
            (Right as IPersistent)?.Save();
        }

        protected override void OnDispose()
        {

        }

    }
}
