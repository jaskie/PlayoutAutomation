using Svt.Caspar.AMCP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Svt.Caspar
{
    public class CasparDevice : IDisposable
    {
        private ServerType _serverType = ServerType.Unknown;

        internal Svt.Network.ServerConnection Connection { get; private set; }
        private Svt.Network.ReconnectionHelper ReconnectionHelper { get; set; }
        private System.Net.IPAddress[] HostAddresses;
        public CasparDeviceSettings Settings { get; private set; }
        public Channel[] Channels { get; private set; }
        public Recorder[] Recorders { get; private set; }
        public string Version { get; private set; }

        public bool IsConnected => Connection != null && Connection.IsConnected;

        public event EventHandler<Svt.Network.ConnectionEventArgs> ConnectionStatusChanged;

        public event EventHandler<DataEventArgs> DataRetrieved;
        public event EventHandler<VersionEventArgs> VersionRetrieved;
        public event EventHandler<EventArgs> UpdatedChannels;
        public event EventHandler<EventArgs> UpdatedRecorders;
        public event EventHandler<Network.Osc.OscPacketEventArgs> OscMessage;

        volatile bool bIsDisconnecting = false;

        public CasparDevice()
        {
            Settings = new CasparDeviceSettings();
            Connection = new Network.ServerConnection();
            Channels = new Channel[0];
            Recorders = new Recorder[0];
            HostAddresses = new System.Net.IPAddress[0];

            Version = "unknown";

            Connection.ProtocolStrategy = new AMCP.AMCPProtocolStrategy(this);
            Connection.ConnectionStateChanged += server__ConnectionStateChanged;
        }

        bool _disposed;
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Disconnect();
            }
        }

        #region OSC

        private void OscListener_PacketReceived(Network.Osc.OscPacketEventArgs e)
        {
            if (HostAddresses.Any(a => a.Equals(e.SourceAddress)))
            {
                OscMessage?.Invoke(this, e);
                var message = e.Packet as Svt.Network.Osc.OscMessage;
                var recorders = Recorders;
                if (message != null)
                    OscMessageReceived(message);
                else
                {
                    var bundle = e.Packet as Svt.Network.Osc.OscBundle;
                    if (bundle != null)
                        foreach (var m in bundle.Messages)
                            OscMessageReceived(m);
                }
            }
        }

        private void OscMessageReceived(Network.Osc.OscMessage message)
        {
            string[] path = message.Address.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (path.Length >= 2)
            {
                int id;
                if (int.TryParse(path[1], out id))
                    switch (path[0])
                    {
                        case "channel":
                            var channels = Channels;
                            channels.FirstOrDefault(c => c.Id == id)?.OscMessage(path, message.Arguments, _serverType);
                            break;
                        case "recorder":
                            var recorders = Recorders;
                            recorders?.FirstOrDefault(r => r.Id == id)?.OscMessage(path, message.Arguments);
                            break;
                    }
            }
        }


        #endregion OSC

        #region Server notifications
        private void server__ConnectionStateChanged(object sender, Network.ConnectionEventArgs e)
        {
            try
            {
                if (ConnectionStatusChanged != null)
                    ConnectionStatusChanged(this, e);
            }
            catch { }

            if (e.Connected)
            {
                Connection.SendRequest("VERSION");
            }
            else
            {
                lock (this)
                {
                    try
                    {
                        if (!bIsDisconnecting && Settings.AutoConnect)
                        {
                            Connection.ConnectionStateChanged -= server__ConnectionStateChanged;
                            ReconnectionHelper = new Svt.Network.ReconnectionHelper(Connection, Settings.ReconnectInterval);
                            ReconnectionHelper.Reconnected += ReconnectionHelper_Reconnected;
                            ReconnectionHelper.Start();
                        }
                    }
                    catch { }
                    bIsDisconnecting = false;
                }
            }
        }

        void ReconnectionHelper_Reconnected(object sender, Network.ConnectionEventArgs e)
        {
            lock (this)
            {
                ReconnectionHelper.Close();
                ReconnectionHelper = null;
                Connection.ConnectionStateChanged += server__ConnectionStateChanged;
            }
            server__ConnectionStateChanged(Connection, e);
        }
        #endregion

        public void SendString(string command)
        {
            if (IsConnected)
                Connection.SendRequest(command);
        }

        #region Connection
        public bool Connect(string host, int port)
        {
            return Connect(host, port, 0, false);
        }

        public bool Connect(string host, int amcpPort, int oscPort, bool reconnect)
        {
            if (!IsConnected)
            {
                Settings.Hostname = host;
                Settings.AmcpPort = amcpPort;
                Settings.OscPort = oscPort;
                Settings.AutoConnect = reconnect;
                return Connect();
            }
            return false;
        }

        public bool Connect()
        {
            if (Settings.OscPort > 0)
            {
                HostAddresses = System.Net.Dns.GetHostAddresses(Settings.Hostname);
                Network.Osc.OscPacketDispatcher.Bind(Settings.OscPort, OscListener_PacketReceived);
            }
            if (!IsConnected)
            {
                Connection.InitiateConnection(Settings.Hostname, Settings.AmcpPort);
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            bIsDisconnecting = true;
            if (ReconnectionHelper != null)
            {
                ReconnectionHelper.Close();
                ReconnectionHelper = null;
                Connection.ConnectionStateChanged += server__ConnectionStateChanged;
            }
            Connection.CloseConnection();
            if (Settings.OscPort > 0)
                Network.Osc.OscPacketDispatcher.UnBind(Settings.OscPort, OscListener_PacketReceived);
        }
        #endregion

        #region AMCP-protocol callbacks
        internal void OnUpdatedChannelInfo(string channelsXml)
        {
            var serializer = new XmlSerializer(typeof(ChannelList));
            using (StringReader reader = new StringReader(channelsXml))
            {
                var newChannels = (ChannelList)serializer.Deserialize(reader);
                if (newChannels.Channels != null)
                    foreach (Channel channel in newChannels.Channels)
                        channel.Connection = this.Connection;
                Channels = newChannels.Channels ?? new Channel[0];

                UpdatedChannels?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void OnUpdatedServerInfo(List<string> channels)
        {
            Channels = channels.Select(s =>
            {
                var channelData = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (channelData.Length > 2
                    && int.TryParse(channelData[0], out var id)
                    && Enum.TryParse($"m{channelData[1]}", true, out VideoMode videoMode))
                    return new Channel(id, videoMode) { Connection = this.Connection };
                return null;
            })
            .Where(c => c != null)
            .ToArray();
            UpdatedChannels?.Invoke(this, EventArgs.Empty);
        }

        internal void OnUpdatedRecorderInfo(string recordersXml)
        {
            var serializer = new XmlSerializer(typeof(RecorderList));
            using (StringReader reader = new StringReader(recordersXml))
            {
                var recorders = (RecorderList)serializer.Deserialize(reader);
                if (recorders.Recorders != null)
                    foreach (Recorder recorder in recorders.Recorders)
                        recorder.Connection = this.Connection;
                Recorders = recorders.Recorders ?? new Recorder[0];
                UpdatedRecorders?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine($"Updated {Connection.Hostname}");
            }
        }

        internal void OnVersion(string version)
        {
            Version = version;
            if (version.StartsWith("2.0"))
            {
                //Ask server for channels
                Connection.SendRequest("INFO SERVER");
                if (version.Contains("TVP"))
                {
                    if (version.Contains("#"))
                    {
                        _serverType = ServerType.V2_0_6_TVP_WithResponseId;
                        Connection.SupportsRequestId = true;
                    }
                    else
                        _serverType = ServerType.V2_0_6_TVP;
                    //Ask server for recorders
                    Connection.SendRequest("INFO RECORDERS");
                }
                else
                    _serverType = ServerType.V2_0;
            }
            else // 2.2 and newer
            {
                _serverType = ServerType.V2_2_Plus;
                Connection.SendRequest("INFO");
            }
            VersionRetrieved?.Invoke(this, new VersionEventArgs(_serverType, version));
        }

        internal void OnDataRetrieved(string data)
        {
            DataRetrieved?.Invoke(this, new DataEventArgs(data));
        }

        internal void OnCommandResponse(string requestId, AMCPCommand command, AMCPError error)
        {
            Connection.SetRequestResponse(requestId, error == AMCPError.None);
        }

        #endregion
    }

    public class DataEventArgs : EventArgs
    {
        public DataEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

    public class VersionEventArgs : EventArgs
    {
        public VersionEventArgs(ServerType serverType, string version)
        {
            ServerType = serverType;
            Version = version;
        }
        public ServerType ServerType { get; }
        public string Version { get; }
    }

    public class CasparDeviceSettings
    {
        public const int DefaultReconnectInterval = 5000;

        public CasparDeviceSettings()
        {
            ReconnectInterval = DefaultReconnectInterval;
        }

        public string Hostname { get; set; }
        public int AmcpPort { get; set; }
        public bool AutoConnect { get; set; }
        public int ReconnectInterval { get; set; }
        public int OscPort { get; set; }
    }

    public enum ServerType
    {
        Unknown,
        /// <summary>
        /// Main branch 2.0
        /// </summary>
        V2_0,
        /// <summary>
        /// TVP's fork up to rev 9.x
        /// </summary>
        V2_0_6_TVP,
        /// <summary>
        /// TVP's fork with request/response ID support, starting with rev 10.0
        /// </summary>
        V2_0_6_TVP_WithResponseId,
        /// <summary>
        /// Main branch 2.2 and newer
        /// </summary>
        V2_2_Plus
    }

}
