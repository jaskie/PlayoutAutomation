using TAS.Common;
using System.Linq;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class Router : RouterBase
    {        
        protected override void Communicator_SourceChanged(object sender, EventArgs<CrosspointInfo> e)
        {
            if (OutputPorts.Length == 0)
                return;

            var port = OutputPorts[0];
            var changedIn = e.Value.OutPort == port ? e.Value : null;
            if (changedIn == null)
                return;

            SelectedSource = Sources.FirstOrDefault(param => param.Id == changedIn.InPort);
        }
    }
}
