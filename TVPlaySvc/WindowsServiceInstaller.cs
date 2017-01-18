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
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = "DokManService";
            serviceInstaller.DisplayName = "Usługa dokuMentor";
            serviceInstaller.Description = "Usługa zapewnia dostęp klientów do danych systemu dokuMentor";
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
