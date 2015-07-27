using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace TAS.Server.Remoting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true), CallbackBehavior()]
    public class Engine : IMediaManager, IEngine
    {
        public void OpenSession()
        {
            throw new NotImplementedException();
        }
    }
}
