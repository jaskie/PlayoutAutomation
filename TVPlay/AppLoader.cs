using System;
using System.Threading.Tasks;
using System.Windows;
using TAS.Client.Views;
using TAS.Server;

namespace TAS.Client
{
    internal class AppLoader
    {
        public static async Task<bool> LoadApp()
        {
            return await Task.Run(() =>
            {
                try
                {
                    SplashScreenView.Current?.Notify("Initializing database...");
                    var database = DatabaseProvider.Database ?? throw new ApplicationException("No database provider loaded");
                    if (database.UpdateRequired())
                    {
                        if (MessageBox.Show(Common.Properties.Resources._message_DatabaseUpdateRequired,
                            Common.Properties.Resources._caption_Warning,
                            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            return false;
                        SplashScreenView.Current?.Notify("Updating database schema...");
                        database.UpdateDb();
                    }
                    SplashScreenView.Current?.Notify("Initializing engines...");
                    EngineController.Current.InitializeEngines();
                    SplashScreenView.Current?.Notify("Loading ingest directories...");
                    EngineController.Current.LoadIngestDirectories();
                }
                catch (TypeInitializationException e)
                {
                    MessageBox.Show(string.Format(Common.Properties.Resources._message_CantInitializeEngines, e.InnerException),
                        Common.Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (Exception e)
                {
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
