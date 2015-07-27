using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace TAS.Server.Remoting
{
    [ServiceContract(CallbackContract = typeof(IMediaManagerCallback))]
    public interface IMediaManager
    {

    }

    public interface IMediaManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPropertyChange();
    }

}
