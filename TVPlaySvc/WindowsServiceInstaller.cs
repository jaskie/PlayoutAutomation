using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace TVPlaySvc
{
    [RunInstaller(true)]
	[DesignerCategory("Code")]
    public class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.DelayedAutoStart = true;
            serviceInstaller.ServiceName = "TVPlaySvc";
            serviceInstaller.DisplayName = "TVPlay - application service";
            serviceInstaller.Description = "Controls play-out automation. Executes scheduled play-out servers commands, provides client access interface.";
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
