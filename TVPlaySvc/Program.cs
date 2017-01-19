using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using TAS.Server;

namespace TVPlaySvc
{
    class TVPlayService : ServiceBase
    {

        public TVPlayService()
        {
            ServiceName = "TVPlay Service";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            EngineController.Initialize();
        }

        protected override void OnStop()
        {
            EngineController.ShutDown();
            base.OnStop();
        }

        protected static void executeApp(bool userInteractive)
        {
            EngineController.Initialize();
            if (userInteractive)
            {
                string line;
                try
                {
                    while (true)
                    {
                        Console.Write('>');
                        line = Console.ReadLine();
                        var lineParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParts.Count() > 0)
                            switch (lineParts[0].ToLower())
                            {
                                // console commands here
                                case "quit":
                                    return;
                                default:
                                    break;
                            }
                    }
                }
                finally
                {
                    EngineController.ShutDown();
                }
            }
        }

        static void Main(string[] args)
        {
            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default:
                        executeApp(true);
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                Run(new TVPlayService());
            }
        }
    }
}
