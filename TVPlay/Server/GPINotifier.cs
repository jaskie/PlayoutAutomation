using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml.Serialization;
using System.Diagnostics;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class GPINotifier : INotifyPropertyChanged, IDisposable, IGpi
    {

        private const int port = 1060;

        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public string Name {get; set;}
        
        public event Action Started;
        GPIState CrawlState = new GPIState();

        [XmlAttribute]
        public int GraphicsStartDelay { get; set; } // may be negative, does not affect aspect ratio switching.

        TcpClient _remoteClient;
        NetworkStream _remoteClientStream;
        Timer _heartbeatTimer;
        int _hearbeatFailCount = 0;
        Thread _socketWorker;

        internal void Initialize()
        {
            Debug.WriteLine(this, "Initializing");
            _hearbeatFailCount = 0;
            try
            {
                _heartbeatTimer = new Timer(new TimerCallback(_heartbeatTick), null, 0, 5000);
                _socketWorker = new Thread(new ThreadStart(_socketWorkerThreadProc));
                _socketWorker.Name = "GPINotifier socket thread worker";
                _socketWorker.Priority = ThreadPriority.AboveNormal;
                _socketWorker.IsBackground = true;
                _socketWorker.Start();
            }
            catch (Exception e)
            {
                Debug.WriteLine(this, e.Message);
            }
        }

        internal void UnInitialize()
        {
            var ht = _heartbeatTimer;
            if (ht != null)
                ht.Dispose();
            _heartbeatTimer = null;
            _remoteDisconnect();
            var sw = _socketWorker;
            if (sw != null)
            {
                _socketWorker.Abort();
                _socketWorker.Join();
            }
            _socketWorker = null;
        }


        private void _remoteConnect()
        {
            _remoteDisconnect();
            _remoteClient = new TcpClient();
            _remoteClient.NoDelay = true;
            _remoteClient.SendTimeout = 1000;
            _remoteClient.SendBufferSize = 256;
            _remoteClient.ReceiveBufferSize = 256;
            try
            {
                var connectAR = _remoteClient.BeginConnect(Address, port, null, null);
                if (connectAR.AsyncWaitHandle.WaitOne(10000))
                {
                    _remoteClientStream = _remoteClient.GetStream();
                    _remoteClientStream.WriteByte((byte)GPICommand.SetIsController);
                    _remoteClientStream.WriteByte((byte)GPICommand.GetInfo);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "GPINotifier client connect error");
                Thread.Sleep(10000);
            }
        }

        private void _remoteDisconnect()
        {
            Connected = false;
            var oldStream = _remoteClientStream;
            if (oldStream != null)
                oldStream.Close();
            var oldClient = _remoteClient;
            if (oldClient != null)
                _remoteClient.Close();
        }

        private void _socketWorkerThreadProc()
        {
            try
            {
                byte[] buffer = new byte[256];
                while (!disposed)
                {
                    int received = 0;
                    var stream = _remoteClientStream;
                    try
                    {
                        if (stream != null && stream.CanRead)
                            received = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (Exception e)
                    {
                        if (e is ThreadAbortException)
                            throw;
                        Debug.WriteLine(e.Message, "GPINotifier client receive error");
                    }
                    if (received > 0)
                        _notificationReceived(buffer, received);
                    else  // client disconnected
                    {
                        if (!disposed)
                            _remoteConnect();
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                Debug.WriteLine(e, "GPINotifier thread aborted");
            }
        }

        private void _notificationReceived(byte[] data, int bufferLength)
        {
            int bufferPos = 0;
            while (bufferLength>bufferPos)
                switch (data[bufferPos])
                {
                    case (byte)GPICommand.HeartBeat:
                        _hearbeatFailCount = 0;
                        Connected = true;
                        bufferPos++;
                        break;
                    case (byte)GPICommand.PlayoutStart:
                        var handler = Started;
                        bufferPos++;
                        if (handler != null)
                        {
                            Debug.WriteLine(this, "PlayoutStart from");
                            handler();
                        }
                        break;
                    case (byte)GPICommand.ShowCrawl:
                        CrawlState.CrawlVisible = true;
                        bufferPos++;
                        NotifyPropertyChanged("Crawl");
                        break;
                    case (byte)GPICommand.HideCrawl:
                        CrawlState.CrawlVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Crawl");
                        break;
                    case (byte)GPICommand.SetCrawl:
                    case (byte)GPICommand.ReloadCrawl:
                        if (bufferLength - bufferPos >= 2)
                        {
                            CrawlState.ConfigNr = data[bufferPos + 1];
                            NotifyPropertyChanged("Crawl");
                            if (data[bufferPos] == (byte)GPICommand.SetCrawl)
                            {
                                CrawlState.CrawlVisible = true;
                                NotifyPropertyChanged("Crawl");
                            }
                        }
                        bufferPos += 2;
                        break;
                    case (byte)GPICommand.ShowLogo0:
                    case (byte)GPICommand.ShowLogo0 + 1:
                    case (byte)GPICommand.ShowLogo0 + 2:
                    case (byte)GPICommand.ShowLogo0 + 3:
                    case (byte)GPICommand.ShowLogo0 + 4:
                    case (byte)GPICommand.ShowLogo0 + 5:
                    case (byte)GPICommand.ShowLogo0 + 6:
                    case (byte)GPICommand.ShowLogo0 + 7:
                    case (byte)GPICommand.ShowLogo0 + 8:
                    case (byte)GPICommand.ShowLogo0 + 9:
                        CrawlState.LogoVisible = true;
                        CrawlState.LogoStyle = (byte)(data[bufferPos] - (byte)GPICommand.ShowLogo0);
                        bufferPos++;
                        NotifyPropertyChanged("Logo");
                        break;
                    case (byte)GPICommand.HideLogo:
                        CrawlState.LogoVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Logo");
                        break;
                    case (byte)GPICommand.ShowParental0:
                    case (byte)GPICommand.ShowParental0 + 1:
                    case (byte)GPICommand.ShowParental0 + 2:
                    case (byte)GPICommand.ShowParental0 + 3:
                    case (byte)GPICommand.ShowParental0 + 4:
                    case (byte)GPICommand.ShowParental0 + 5:
                    case (byte)GPICommand.ShowParental0 + 6:
                    case (byte)GPICommand.ShowParental0 + 7:
                    case (byte)GPICommand.ShowParental0 + 8:
                    case (byte)GPICommand.ShowParental0 + 9:
                        CrawlState.ParentalVisible = true;
                        CrawlState.ParentalStyle = (byte)(data[bufferPos] - (byte)GPICommand.ShowParental0);
                        bufferPos++;
                        NotifyPropertyChanged("Parental");
                        break;
                    case (byte)GPICommand.HideParental:
                        CrawlState.ParentalVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Parental");
                        break;
                    case (byte)GPICommand.AspectNarrow:
                        CrawlState.AspectNarrow = true;
                        bufferPos++;
                        NotifyPropertyChanged("AspectNarrow");
                        break;
                    case (byte)GPICommand.AspectWide:
                        CrawlState.AspectNarrow = false;
                        bufferPos++;
                        NotifyPropertyChanged("AspectNarrow");
                        break;
                    case (byte)GPICommand.MasterTake:
                        _isMaster = true;
                        bufferPos++;
                        NotifyPropertyChanged("IsMaster");
                        break;
                    case (byte)GPICommand.MasterFree:
                        _isMaster = false;
                        bufferPos++;
                        NotifyPropertyChanged("IsMaster");
                        break;
                    case (byte)GPICommand.GetInfo:
                        if (bufferLength - bufferPos >= 6 && data[bufferPos + 1] >= bufferLength - 2)
                        {
                            lock (CrawlState.SyncRoot)
                            {

                                byte[] temp = new byte[data.Length - 8];
                                Array.Copy(data, 8, temp, 0, temp.Length);
                                CrawlState.VisibleAuxes.Clear();
                                CrawlState.VisibleAuxes.AddRange(temp);
                                CrawlState.AspectNarrow = data[2] != 0;
                                CrawlState.ConfigNr = data[3];
                                CrawlState.CrawlVisible = data[4] != 0;
                                CrawlState.LogoVisible = data[5] != 0;
                                CrawlState.LogoStyle = (byte)(data[5] == 0 ? 0 : data[5] - 1);
                                CrawlState.ParentalVisible = data[6] != 0;
                                CrawlState.LogoStyle = (byte)(data[6] == 0 ? 0 : data[6] - 1);
                                CrawlState.Mono = data[7] != 0;
                            }
                            NotifyPropertyChanged("Logo");
                            NotifyPropertyChanged("Parental");
                            NotifyPropertyChanged("AspectNarrow");
                            NotifyPropertyChanged("Crawl");
//                            NotifyPropertyChanged("CrawlVisible");
                            bufferPos += (byte)(data[bufferPos + 1] + 2);
                        }
                        else
                            bufferPos = bufferLength;
                        break;
                    default:
                        bufferPos = bufferLength;
                        break;
                }
        }

        private object _timerSync = new object();
        private void _heartbeatTick(object state)
        {
            lock (_timerSync)
            {
                var stream = _remoteClientStream;
                try
                {
                    if (stream != null && stream.CanWrite)
                        stream.WriteByte((byte)GPICommand.HeartBeat);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message, "Heartbeat not sended");
                }
                _hearbeatFailCount++;
                if (_hearbeatFailCount > 3)
                {
                    Connected = false;
                }
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                UnInitialize();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }


        private bool _connected = false;
        [XmlIgnore]
        public bool Connected
        {
            get { return _connected; }
            protected set
            {
                if (value != _connected)
                {
                    _connected = value; 
                    NotifyPropertyChanged("Connected");
                    NotifyPropertyChanged("IsMaster");
                }
            }
        }

        private void _sendCommand(byte command, params byte[] param )
        {
            var stream = _remoteClientStream;
            try
            {
                byte[] toWrite = new byte[param.Length+1];
                toWrite[0] = command;
                for (int i = 0; i < param.Length; i++)
                    toWrite[i + 1] = param[i];
                    if (stream != null && stream.CanWrite)
                        stream.Write(toWrite, 0, param.Length+1);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Command {0} not sended - error {1}", command, e);
            }
        }

        [XmlIgnore]
        public bool AspectNarrow
        {
            get { return CrawlState.AspectNarrow; }
            set
            {
                if (CrawlState.AspectNarrow != value)
                {
                    if (value)
                        _sendCommand((byte)GPICommand.AspectNarrow);
                    else
                        _sendCommand((byte)GPICommand.AspectWide);
                }
            }
        }

        [XmlIgnore]
        public bool CrawlVisible
        {
            get { return CrawlState.CrawlVisible; }
            set
            {
                if (CrawlState.CrawlVisible != value)
                    _sendCommand(value ? (byte)GPICommand.ShowCrawl : (byte)GPICommand.HideCrawl);
            }
        }

        [XmlIgnore]
        public int Crawl
        {
            get { return CrawlState.CrawlVisible ? CrawlState.ConfigNr : 0; }
            set
            {
                if (Crawl != value)
                {
                    if (value == 0)
                        _sendCommand((byte)GPICommand.HideCrawl);
                    else
                        _sendCommand((byte)GPICommand.SetCrawl, (byte)value);
                }
            }
        }

        [XmlIgnore]
        public int Logo
        {
            get
            {
                if (!CrawlState.LogoVisible)
                    return 0;
                else
                    return CrawlState.LogoStyle + 1;
            }
            set
            {
                if (Logo != value)
                {
                    if (value == 0)
                        _sendCommand((byte)GPICommand.HideLogo);
                    else
                        _sendCommand((byte)((byte)GPICommand.ShowLogo0 + (value - 1)));
                }
            }
        }

        [XmlIgnore]
        public int Parental
        {
            get
            {
                if (!CrawlState.ParentalVisible)
                    return 0;
                else
                    return (CrawlState.ParentalStyle + 1);
            }
            set
            {
                if (Parental != value)
                {
                    if (value == 0)
                        _sendCommand((byte)GPICommand.HideParental);
                    else
                        _sendCommand((byte)((byte)GPICommand.ShowParental0 + (value - 1)));
                }
            }
        }


        private bool _isMaster;
        public bool IsMaster
        {
            get { return _isMaster; }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? base.ToString() : Name;
        }
    }

}
