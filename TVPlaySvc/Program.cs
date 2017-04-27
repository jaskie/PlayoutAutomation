using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
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
                                case "q":
                                    return;
                                case "gc":
                                    Debug.WriteLine("Garbage collection requested.");
                                    GC.Collect(GC.MaxGeneration);
                                    Console.WriteLine("Garbage collection requested.");
                                    break;
                                case "help":
                                case "?":
                                    DisplayHelpInfo();
                                    break;
                                default:
                                    Console.WriteLine("Command not recognized. Type help to get list of available commands.");
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

        private static void DisplayHelpInfo()
        {
            Console.Write(@"Avaliable commands:
    quit, q - exits application,
    gc      - forces garbage collection,
    help, ? - display this info.
");
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
