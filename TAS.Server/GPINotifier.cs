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
using TAS.Server.Interfaces;

namespace TAS.Server
{
    [Serializable]
    public class GPINotifier : INotifyPropertyChanged, IDisposable, IGpi, IGpiConfig
    {

        private const int port = 1060;

        #region IGpiConfig
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public int GraphicsStartDelay { get; set; } // may be negative, does not affect aspect ratio switching.
        #endregion //IGpiCofig

        public event Action Started;
        GPIState CrawlState = new GPIState();


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
                    if (_remoteClient.Connected)
                    {
                        _remoteClientStream = _remoteClient.GetStream();
                        _remoteClientStream.WriteByte((byte)GPICommand.SetIsController);
                    }
                    else
                        Thread.Sleep(10000);
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
                            NotifyPropertyChanged(nameof(Logo));
                            NotifyPropertyChanged(nameof(Parental));
                            NotifyPropertyChanged(nameof(AspectNarrow));
                            NotifyPropertyChanged(nameof(Crawl));
                            NotifyPropertyChanged(nameof(CrawlVisible));
                            bufferPos += (byte)(data[bufferPos + 1] + 2);
                        }
                        else
                            bufferPos = bufferLength;
                        break;
                    case (byte)GPICommand.ShowCrawl:
                        CrawlState.CrawlVisible = true;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(CrawlVisible));
                        break;
                    case (byte)GPICommand.HideCrawl:
                        CrawlState.CrawlVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(CrawlVisible));
                        break;
                    case (byte)GPICommand.SetCrawl:
                    case (byte)GPICommand.ReloadCrawl:
                        if (bufferLength - bufferPos >= 2)
                        {
                            CrawlState.ConfigNr = data[bufferPos + 1];
                            NotifyPropertyChanged(nameof(Crawl));
                            if (data[bufferPos] == (byte)GPICommand.SetCrawl)
                            {
                                CrawlState.CrawlVisible = true;
                                NotifyPropertyChanged(nameof(CrawlVisible));
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
                        NotifyPropertyChanged(nameof(Logo));
                        break;
                    case (byte)GPICommand.HideLogo:
                        CrawlState.LogoVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(Logo));
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
                        NotifyPropertyChanged(nameof(Parental));
                        break;
                    case (byte)GPICommand.HideParental:
                        CrawlState.ParentalVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(Parental));
                        break;
                    case (byte)GPICommand.AspectNarrow:
                        CrawlState.AspectNarrow = true;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(AspectNarrow));
                        break;
                    case (byte)GPICommand.AspectWide:
                        CrawlState.AspectNarrow = false;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(AspectNarrow));
                        break;
                    case (byte)GPICommand.MasterTake:
                        _isMaster = true;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(IsMaster));
                        break;
                    case (byte)GPICommand.MasterFree:
                        _isMaster = false;
                        bufferPos++;
                        NotifyPropertyChanged(nameof(IsMaster));
                        break;
                    case (byte)GPICommand.AuxShow:
                    case (byte)GPICommand.AuxHide:
                        if (bufferLength - bufferPos >= 2)
                        {
                            var auxNr = (int)data[bufferPos + 1];
                            lock (_visibleAuxes.SyncRoot)
                            {
                                bool contanis = _visibleAuxes.Contains(auxNr);
                                if (!contanis && data[bufferPos] == (byte)GPICommand.AuxShow)
                                    _visibleAuxes.Add(auxNr);
                                if (contanis && data[bufferPos] == (byte)GPICommand.AuxHide)
                                    _visibleAuxes.Remove(auxNr);
                            }
                            NotifyPropertyChanged(nameof(VisibleAuxes));
                        }
                        bufferPos += 2;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    NotifyPropertyChanged(nameof(Connected));
                    NotifyPropertyChanged(nameof(IsMaster));
                }
            }
        }

        private void _sendCommand(GPICommand command, params byte[] param )
        {
            var stream = _remoteClientStream;
            try
            {
                byte[] toWrite = new byte[param.Length+1];
                toWrite[0] = (byte)command;
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
                        _sendCommand(GPICommand.AspectNarrow);
                    else
                        _sendCommand(GPICommand.AspectWide);
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
                    _sendCommand(value ? GPICommand.ShowCrawl : GPICommand.HideCrawl);
            }
        }

        [XmlIgnore]
        public int Crawl
        {
            get { return CrawlState.ConfigNr; }
            set
            {
                if (Crawl != value)
                    _sendCommand(GPICommand.SetCrawl, (byte)value);
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
                        _sendCommand(GPICommand.HideLogo);
                    else
                        _sendCommand((GPICommand)((byte)GPICommand.ShowLogo0 + (value - 1)));
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
                        _sendCommand(GPICommand.HideParental);
                    else
                        _sendCommand((GPICommand)((byte)GPICommand.ShowParental0 + (value - 1)));
                }
            }
        }

        private readonly SynchronizedCollection<int> _visibleAuxes = new SynchronizedCollection<int>();
        [XmlIgnore]
        public int[] VisibleAuxes { get { lock (_visibleAuxes.SyncRoot) return _visibleAuxes.ToArray(); } }

        public void ShowAux(int auxNr)
        {
            _sendCommand(GPICommand.AuxShow, (byte)auxNr);
        }
        public void HideAux(int auxNr)
        {
            _sendCommand(GPICommand.AuxHide, (byte)auxNr);
        }

        private bool _isMaster;
        public bool IsMaster
        {
            get { return _isMaster; }
        }

        public override string ToString()
        {
            return Address;
        }
    }

}
