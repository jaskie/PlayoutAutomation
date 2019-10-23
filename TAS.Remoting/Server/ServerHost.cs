using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
using System.Xml.Serialization;
using NLog;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Server
{
    public class ServerHost : IDisposable, IRemoteHostConfig
    {
        private int _disposed;
        private TcpListener _listener;
        private Thread _listenerThread;
        private IDto _rootDto;
        private Func<IPAddress, IPrincipal> _findUserFunc;
        private readonly List<ServerSession> _clients = new List<ServerSession>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        [XmlAttribute]
        public ushort ListenPort { get; set; }

        public bool Initialize(DtoBase rootDto, Func<IPAddress, IPrincipal> findUserFunc)
        {
            if (ListenPort < 1024)
                return false;
            _rootDto = rootDto;
            _findUserFunc = findUserFunc;
            try
            {
                _listener = new TcpListener(IPAddress.Any, ListenPort) {ExclusiveAddressUse = true};
                _listenerThread = new Thread(ListenerThreadProc)
                {
                    Name = $"Remote client session listener on port {ListenPort}",
                    IsBackground = true
                };
                _listenerThread.Start();
                return true;
            }
            catch(Exception e)
            {
                Logger.Error(e, "Initialization of {0} error.", this);
            }
            return false;
        }

        private void ListenerThreadProc()
        {
            try
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
                            Logger.Trace("{0} shutdown.", this);
                            break;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Logger.Warn("{0} Unauthorized client from: {1}", this, client?.Client.RemoteEndPoint);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, "{0} unexpected listener thread exception", this);
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
            catch (Exception e)
            {
                Logger.Error(e, "{0} general error", this);
            }
        }

        private void AddClient(TcpClient client)
        {
            var clientSession = new ServerSession(client, _rootDto, _findUserFunc);
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

        public override string ToString()
        {
            return $"ServerHost on {ListenPort}";
        }
    }
}
