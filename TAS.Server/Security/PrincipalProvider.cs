using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using jNet.RPC.Server;
using TAS.Common;
using TAS.Common.Interfaces.Security;

namespace TAS.Server.Security
{
    internal class PrincipalProvider: IPrincipalProvider
    {
        private readonly IAuthenticationService _authenticationService;

        public PrincipalProvider(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        public IPrincipal GetPrincipal(TcpClient client)
        {
            if (!(client.Client?.RemoteEndPoint is IPEndPoint endPoint))
                return null;
            var user = _authenticationService.FindUser(AuthenticationSource.IpAddress, endPoint.Address.ToString());
            return user == null ? null : new GenericPrincipal(user, new string[0]);
        }
    }
}
