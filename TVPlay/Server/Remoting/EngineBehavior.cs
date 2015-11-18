using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using WebSocketSharp.Server;

namespace TAS.Server.Remoting
{
    public class EngineBehavior: WebSocketBehavior
    {
        private readonly Engine _engine;
        public EngineBehavior(Engine engine)
        {
            _engine = engine;
        }
    }
}
