using System.Net;
using System.Security.Claims;

namespace TAS.Server.Security
{
    public class IpAddressPrincipal: ClaimsPrincipal
    {
        public IpAddressPrincipal(IPAddress address)
        {
            Address = address;
        }

        public IPAddress Address { get; }

    }
}
