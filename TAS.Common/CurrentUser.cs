using System.Threading;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Common
{
    public static class CurrentUser
    {
        public static bool IsAdmin => User?.IsAdmin == true;
        public static IUser User => Thread.CurrentPrincipal.Identity as IUser;
    }
}
