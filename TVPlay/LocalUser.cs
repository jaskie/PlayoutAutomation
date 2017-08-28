using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client
{
    internal class LocalUser: IUser
    {
        public SecurityObjectType SecurityObjectTypeType { get; } = SecurityObjectType.User;

        public string Name { get; set; } = Properties.Resources._localUserName;

        public string AuthenticationType { get; } = AuthenticationSource.Console.ToString();

        public bool IsAuthenticated { get; } = true;

        public bool IsAdmin { get; set; } = true;

        public AuthenticationSource AuthenticationSource { get; set; } = AuthenticationSource.Console;

        public string AuthenticationObject { get; set; } = AuthenticationSource.Console.ToString();

        public ulong Id { get; set; }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<IGroup> Groups { get; } = new List<IGroup>();

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
