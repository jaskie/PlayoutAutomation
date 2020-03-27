using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using TAS.Server;

namespace TVPlaySvc
{
    [System.ComponentModel.DesignerCategory("Code")]
    internal class TvPlayService : ServiceBase
    {

        public TvPlayService()
        {
            ServiceName = "TVPlay Service";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            StartUp();
        }

        protected override void OnStop()
        {
            EngineController.Current.ShutDown();
            base.OnStop();
        }

        private static void StartUp()
        {
            var database = DatabaseProvider.Database ?? throw new ApplicationException("No database provider loaded");
            if (database.UpdateRequired())
                database.UpdateDb();
            EngineController.Current.InitializeEngines();
            EngineController.Current.LoadIngestDirectories();
        }

        protected static void ExecuteApp()
        {
            try
            {
                StartUp();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
                return;
            }
            try
            {
                while (true)
                {
                    Console.Write('>');
                    var line = Console.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;
                    var lineParts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Length > 0)
                        switch (lineParts[0].ToLower())
                        {
                            // console commands here
                            case "quit":
                            case "q":
                                return;
#if DEBUG
                            case "gc":
                                GC.Collect(GC.MaxGeneration);
                                Console.WriteLine("Garbage collection requested.");
                                break;
#endif
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
                EngineController.Current.ShutDown();
            }
        }

        private static void DisplayHelpInfo()
        {
            Console.Write(@"Avaliable commands:
    quit, q - exits application,"
#if DEBUG
+@"
    gc      - forces garbage collection,"
#endif
+@"
    help, ? - display this info.
"
);
        }

        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                    default:
                        ExecuteApp();
                        Environment.Exit(0);
                        break;
                }
            }
            else
            {
                Run(new TvPlayService());
            }
        }
    }
}
