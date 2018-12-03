using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Client.Views;
using TAS.Server;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewModelBase
    {

        public MainWindowViewmodel()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;
            try
            {
                SplashScreenView.Current?.Notify("Initializing engines...");
                EngineController.Initialize();
                Tabs = new List<ViewModelBase>(
                    EngineController.Engines.Select(engine =>
                    {
                        SplashScreenView.Current?.Notify($"Creating {engine.EngineName}...");
                        return new ChannelViewmodel(engine, true, true);
                    }));
            }
            catch (TypeInitializationException e)
            {
                MessageBox.Show(string.Format(resources._message_CantInitializeEngines, e.InnerException),
                    resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(string.Format(resources._message_CantInitializeEngines, message),
                    resources._caption_Error,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }

        public List<ViewModelBase> Tabs { get; } = new List<ViewModelBase>();

        protected override void OnDispose()
        {
            Tabs.ToList().ForEach(c => c.Dispose());
            EngineController.ShutDown();
        }
    }
}
