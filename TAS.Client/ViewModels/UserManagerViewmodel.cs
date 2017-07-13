using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserManagerViewmodel: ViewmodelBase
    {
        public UserManagerViewmodel()
        {
            Users = new ObservableCollection<IUser>();
        }

        public string DisplayName { get; } = Common.Properties.Resources._users;
        protected override void OnDispose()
        {
            
        }

        public ObservableCollection<IUser> Users { get; }
    }
}
