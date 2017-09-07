using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace TAS.Client.ViewModels
{
    public class SplashScreenViewmodel: INotifyPropertyChanged
    {
        public static SplashScreenViewmodel Current { get; private set; }
        public SplashScreenViewmodel()
        {
            Current = this;
            var assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string LoadStage { get; private set; }

        public string Version { get; }

        public void SetLoadStage(string stage)
        {
            LoadStage = stage;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadStage)));
        }
    }
}
