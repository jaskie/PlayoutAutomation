using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using NLog;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Security;

namespace TAS.Remoting.Server
{
    public class ServerHost : IDisposable, IRemoteHostConfig
    {
        private int _disposed;
        private TcpListener _listener;
        private Thread _listenerThread;
        private IDto _rootDto;
        private IAuthenticationService _authenticationService;
        private readonly List<ServerSession> _clients = new List<ServerSession>();
        private static readonly Logger Logger = LogManager.GetLogger(nameof(ServerHost));
        
        [XmlAttribute]
        public ushort ListenPort { get; set; }

        public bool Initialize(DtoBase rootDto, string path, IAuthenticationService authenticationService)
        {
            if (ListenPort < 1024)
                return false;
            _rootDto = rootDto;
            _authenticationService = authenticationService;
            try
            {
                _listener = new TcpListener(IPAddress.Any, ListenPort) {ExclusiveAddressUse = true};
                _listenerThread = new Thread(ListenerThreadProc)
                {
                    Name = "Remote client session listener",
                    IsBackground = true
                };
                _listenerThread.Start();
                return true;
            }
            catch(Exception e)
            {
                Logger.Error(e, "Initialization of ServerHost error.");
            }
            return false;
        }

        private void ListenerThreadProc()
        {
            _listener.Start();
            try
            {
                while (true)
                {
                    TcpClient client = null;
                    try
                    {
                        client = _listener.AcceptTcpClient();
                        AddClient(client);
                    }
                    catch (Exception e) when (e is SocketException || e is ThreadAbortException)
                    {
                        Logger.Trace("ServerHost shutdown.");
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Logger.Warn($"Unauthorized client from: {client?.Client.RemoteEndPoint}");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Unexpected listener thread exception");
                    }
                }
            }
            finally
            {
                _listener.Stop();
                List<ServerSession> serverSessionsCopy;
                lock (((IList) _clients).SyncRoot)
                    serverSessionsCopy = _clients.ToList();
                serverSessionsCopy.ForEach(s => s.Dispose());
            }
        }

        private void AddClient(TcpClient client)
        {
            var clientSession = new ServerSession(client, _authenticationService, _rootDto);
            clientSession.Disconnected += ClientSessionDisconnected;
            lock (((IList)_clients).SyncRoot)
                _clients.Add(clientSession);
        }

        private void ClientSessionDisconnected(object sender, EventArgs e)
        {
            var serverSession = sender as ServerSession ?? throw new ArgumentException(nameof(sender));
            lock (((IList) _clients).SyncRoot)
                _clients.Remove(serverSession);
            serverSession.Disconnected -= ClientSessionDisconnected;
            serverSession.Dispose();
        }

        public int ClientCount
        {
            get
            {
                lock (((IList) _clients).SyncRoot)
                    return _clients.Count;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == default(int))
                UnInitialize();
        }

        public void UnInitialize()
        {
            _listenerThread.Abort();
        }
    }
}
