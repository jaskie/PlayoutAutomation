using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TAS.Server.Remoting;

namespace TAS.Client.Model
{
    public interface IRemoteClient
    {
        event EventHandler<WebSocketMessageEventArgs> OnMessage;
        T Query<T>(DtoBase dto, [CallerMemberName] string methodName = "", params object[] parameters);
    }
}
