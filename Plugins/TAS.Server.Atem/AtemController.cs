using jNet.RPC.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class AtemController : ServerObjectBase, IRouter, IDisposable
    {
        private readonly LibAtem.Net.AtemClient _atemClient;
        private bool _isConnected;

        public AtemController(AtemDevice atemDevice)
        {
            _atemClient = new LibAtem.Net.AtemClient(atemDevice.Address, false);
            _atemClient.OnConnection += OnConnection;
            _atemClient.OnDisconnect += OnDisconnect;
            _atemClient.Connect();
        }

        public IList<IRouterPort> InputPorts => throw new NotImplementedException();

        public IRouterPort SelectedInputPort => throw new NotImplementedException();

        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        public void SelectInput(int inputId)
        {
        }

        private void OnConnection(object sender) => IsConnected = true;
        private void OnDisconnect(object sender) => IsConnected = false;

        public void Dispose()
        {
            _atemClient.Dispose();
        }
    }
}
