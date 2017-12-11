using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DokMan.Svc
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public WindowsServiceInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

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
