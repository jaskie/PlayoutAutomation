using System;
using System.Threading.Tasks;
using System.Windows;
using TAS.Client.Views;
using TAS.Server;

namespace TAS.Client
{
    internal class AppLoader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static async Task<bool> LoadApp()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var splash = SplashScreenView.Current;
                    splash?.Notify("Initializing database...");
                    var database = DatabaseProvider.Database ?? throw new ApplicationException("No database provider loaded");
                    if (database.UpdateRequired())
                    {
                        if (MessageBox.Show(Common.Properties.Resources._message_DatabaseUpdateRequired,
                            Common.Properties.Resources._caption_Warning,
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            return false;
                        splash?.Notify("Updating database schema...");
                        database.UpdateDb();
                    }
                    splash?.Notify("Initializing engines...");
                    EngineController.Current.InitializeEngines();
                    splash?.Notify("Loading ingest directories...");
                    EngineController.Current.LoadIngestDirectories();
                }
                catch (TypeInitializationException e)
                {
                    Logger.Error(e, "Can't initialize application");
                    MessageBox.Show(string.Format(Common.Properties.Resources._message_CantInitializeEngines, e.InnerException),
                        Common.Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Can't load application");
                    var exceptionToShow = e;
                    while (exceptionToShow.InnerException != null)
                        exceptionToShow = exceptionToShow.InnerException;
                    var message =
#if DEBUG
                    $"{e}";
#else
                $"{exceptionToShow.GetType().Name} {exceptionToShow.Message}";
#endif
                    MessageBox.Show(string.Format(Common.Properties.Resources._message_CantInitializeEngines, message),
                        Common.Properties.Resources._caption_Error,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return true;
            }
            );
        }
    }
}
