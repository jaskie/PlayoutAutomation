using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client
{
    internal class LocalUser: IUser
    {
        public SceurityObjectType SceurityObjectTypeType { get; } = SceurityObjectType.User;

        public string Name { get; set; } = Properties.Resources._localUserName;

        public string AuthenticationType { get; } = AuthenticationTypes.Local;

        public bool IsAuthenticated { get; } = true;

        public ulong Id { get; set; }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public IList<IGroup> Groups { get; } = new List<IGroup>();

        public void GroupAdd(IGroup group)
        {
            throw new NotImplementedException();
        }

        public bool GroupRemove(IGroup group)
        {
            throw new NotImplementedException();
        }
    }
}
