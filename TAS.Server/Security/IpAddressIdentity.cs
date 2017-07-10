using System.Security.Principal;

namespace TAS.Server.Security
{
    public class IpAddressIdentity: IIdentity
    {
        public string Name { get; } = nameof(IpAddressIdentity);
        public string AuthenticationType { get; } = "IP address";
        public bool IsAuthenticated { get; } = true;
    }
}
