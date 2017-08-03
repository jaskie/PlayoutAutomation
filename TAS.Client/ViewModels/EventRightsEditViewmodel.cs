using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventRightsEditViewmodel: ViewmodelBase
    {
        private readonly IEvent _ev;
        private readonly IAuthenticationService _authenticationService;
        private readonly List<IAclItem> _rights;

        public EventRightsEditViewmodel(IEvent ev, IAuthenticationService authenticationService)
        {
            _ev = ev;
            _authenticationService = authenticationService;
            AllUsersAndGroups = authenticationService.Users.Cast<ISecurityObject>().Concat(authenticationService.Groups).ToArray();
            _rights = ev.Rights;
        }

        protected override void OnDispose()
        {
            
        }

        public ISecurityObject[] AllUsersAndGroups { get; }


        public void Save()
        {
            
        }
    }
}
