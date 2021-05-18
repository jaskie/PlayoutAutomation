using System;
using TAS.Common;
using System.Linq;

namespace TAS.Server.VideoSwitch.Model
{	    
    public class Router : RouterBase
    {        
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();                                          

        public Router(CommunicatorType type = CommunicatorType.None) : base(type)
        {         
            if (Communicator != null)
                Communicator.SourceChanged += Communicator_OnInputPortChangeReceived;                                  
        }                                                                                                 

        //private void Communicator_OnRouterPortStateReceived(object sender, EventArgs<PortState[]> e)
        //{
        //    foreach (var port in Sources)
        //        ((RouterPort)port).IsSignalPresent = e.Value?.FirstOrDefault(param => param.PortId == port.PortId)?.IsSignalPresent;
        //}

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            if (OutputPorts.Length == 0)
                return;

            var port = OutputPorts[0];
            var changedIn = e.Value.OutPort == port ? e.Value : null;
            if (changedIn == null)
                return;

            SelectedSource = Sources.FirstOrDefault(param => param.Id == changedIn.InPort);
        }

        protected override void Dispose(bool disposing)
        {
            if (Communicator != null)
                Communicator.SourceChanged -= Communicator_OnInputPortChangeReceived;
            base.Dispose(disposing);
        }
    }
}
