using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace TAS.Server.Remoting
{
    [ServiceContract(CallbackContract = typeof(IEngineCallback))]
    public interface IEngine
    {
        [OperationContract]
        void OpenSession();
    }


    public interface IEngineCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnPropertyChange();
    }
}
