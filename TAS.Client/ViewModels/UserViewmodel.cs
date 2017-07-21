using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class UserViewmodel: ViewmodelBase
    {
        private readonly IUser _user;
        private string _userName;

        public UserViewmodel(IUser user)
        {
            _user = user;
        }

        public string UserName
        {
            get { return _userName; }
            set { SetField(ref _userName, value); }
        }

        public void Save()
        {
            _user.Name = UserName;
            _user.Save();
        }

        public void Delete()
        {
            _user.Delete();
        }

        protected override void OnDispose()
        {
            
        }
    }
}
