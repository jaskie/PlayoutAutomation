using System.ComponentModel;
using System.Linq;
using System.Threading;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class NevionConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private bool _preload;
        private string _login;
        private string _password;
        private int _level;
        private readonly Nevion _nevion;

        public NevionConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _nevion = engine.VideoSwitch as Nevion ?? new Nevion();
            _nevion.PropertyChanged += Nevion_PropertyChanged;
            Load();
        }

        public override string PluginName => "Nevion video router";

        public override IPlugin Model => _nevion;

        private void Nevion_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedSource));
            else if (e.PropertyName == nameof(IVideoSwitch.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        protected override bool CanConnect() => IpAddress?.Length > 0;


        protected override void Connect()
        {
            _nevion.IpAddress = IpAddress;
            _nevion.Login = _login;
            _nevion.Password = _password;
            _nevion.Level = _level;
            _nevion.OutputPorts = SelectedOutputPorts.Select(p => p.Id).ToArray();

            _nevion.Connect(CancellationToken.None);
        }

        protected override void Disconnect()
        {
            _nevion.Disconnect();
        }

        public override void Load()
        {
            base.Load();
            SelectedOutputPorts.Clear();
            foreach (var source in _nevion.Sources.Select(p => new PortInfo(p.Id, p.Name)))
                SelectedOutputPorts.Add(source);
            Level = _nevion.Level;
            IpAddress = _nevion.IpAddress;
            Login = _nevion.Login;
            Password = _nevion.Password;
            Preload = _nevion.Preload;
            IsModified = false;
        }

        protected override void OnDispose()
        {
            _nevion.PropertyChanged -= Nevion_PropertyChanged;
            _nevion.Dispose();
        }

        public override void Save()
        {
            base.Save();
            _nevion.Level = Level;
            _nevion.IpAddress = IpAddress;
            _nevion.Login = Login;
            _nevion.Password = Password;
            _nevion.Preload = Preload;
            Engine.VideoSwitch = _nevion;
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && IpAddress?.Length>0 && _login?.Length > 0 && _password?.Length > 0)
                return true;
            return false;
        }

        public string Login { get => _login; set => SetField(ref _login, value); }
        public string Password { get => _password; set => SetField(ref _password, value); }
        public int Level { get => _level; set => SetField(ref _level, value); }
        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedSource
        {
            get => _nevion.SelectedSource;
            set
            {
                if (_nevion?.SelectedSource == value)
                    return;
                _nevion?.SetSource(value.Id);
            }
        }

        public override bool IsVideoSwitcher => false;
    }
}
