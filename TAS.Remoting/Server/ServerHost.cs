using System;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using TAS.Common.Interfaces;
using WebSocketSharp.Server;
using Newtonsoft.Json.Serialization;

namespace TAS.Server
{
    public class ServerHost : IDisposable, IRemoteHostConfig
    {
        private WebSocketServer _server;
        private int _disposed;

        [XmlAttribute]
        public ushort ListenPort { get; set; }

        public bool Initialize(DtoBase dto, string path, IAuthenticationService authenticationService)
        {
            if (ListenPort < 1024)
                return false;
            try
            {
                _server = new WebSocketServer(ListenPort) {NoDelay = true};
                _server.AddWebSocketService<ServerSession>(path, s =>
                {
                    s.AuthenticationService = authenticationService;
                    s.InitialObject = dto;
                });
                _server.Start();
                return true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e, "Initialization of RemoteClientHost error");
            }
            return false;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
                UnInitialize();
        }

        public void UnInitialize()
        {
            _server?.Stop();
        }
    }
}
