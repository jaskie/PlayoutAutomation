using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TAS.Server.Remoting
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true), CallbackBehavior]
    public class Engine : IMediaManager, IEngine
    {
        public IMediaManagerCallback MediaManagerCallback;
        public void OpenSession()
        {
            MediaManagerCallback = OperationContext.Current.GetCallbackChannel<IMediaManagerCallback>();
            Debug.WriteLine("Remote interface connected");
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                MediaManagerCallback.OnPropertyChange();
            });
        }
    }
}
