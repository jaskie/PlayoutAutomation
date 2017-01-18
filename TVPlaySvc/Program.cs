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
    class Program : ServiceBase
    {
        public Program()
        {
            ServiceName = "TVPlay Service";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            executeApp(false);
        }

        protected override void OnStop()
        {
            EngineController.ShutDown();
            base.OnStop();
        }

        protected static void executeApp(bool userInteractive)
        {
            var engines = EngineController.Engines;
            if (userInteractive)
            {
                string line;
                do
                {
                    Console.Write('>');
                    line = Console.ReadLine();
                    var lineParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Count() > 0)
                        switch (lineParts[0].ToLower())
                        {
                            case "q":
                            case "quit":
                                break;
                            default:
                                break;
                        }
                } while (line != "q" && line != "quit");
                EngineController.ShutDown();
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
                Run(new Program());
            }
        }
    }
}
