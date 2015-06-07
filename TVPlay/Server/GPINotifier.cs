using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Automation.BDaq;
using System.Xml.Serialization;
using System.Diagnostics;
using System.ComponentModel;
using System.Net.Sockets;
using TVP.Crawl.RestServer;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace TAS.Server
{
    [Serializable]
    public class GPINotifier : INotifyPropertyChanged, IDisposable
    {

        private const int port = 1060;

        [XmlAttribute]
        public GPIType Type { get; set; }
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public string Name {get; set;}
        public InputPin InputPinStart { get; set; }
        public InputPin InputPinPause { get; set; }
        public event Action StartPressed;
        CrawlState CrawlState = new CrawlState();

        [XmlAttribute]
        public int GraphicsStartDelay { get; set; } // may be negative, does not affect aspect ratio switching.



        InstantDiCtrl _instantDiCtrlStart;
        TcpClient _remoteClient;
        NetworkStream _remoteClientStream;
        Timer _heartbeatTimer;
        int _hearbeatFailCount = 0;
        Thread _socketWorker;

        internal void Initialize()
        {
            Debug.WriteLine(this, "Initializing");
            try
            {
                if (Type == GPIType.Advantech)
                {
                    if (InputPinStart != null)
                    {
                        Debug.WriteLine(this, "StartPin new InstantDiCtrl()");
                        _instantDiCtrlStart = new InstantDiCtrl();
                        Debug.WriteLine(this, "StartPin new DeviceInformation()");
                        _instantDiCtrlStart.Interrupt += new EventHandler<DiSnapEventArgs>(instantDiCtrlStart_Interrupt);
                        _instantDiCtrlStart.SelectedDevice = new DeviceInformation(InputPinStart.DeviceId);
                        DiintChannel[] diintChannels = _instantDiCtrlStart.DiintChannels;
                        if (diintChannels != null)
                        {
                            diintChannels[0].Enabled = true;
                            Debug.WriteLine(this, "Channel 0 enabled");
                        }
                        Debug.WriteLine(this, "StartPin SnapStart");
                        ErrorCode err = _instantDiCtrlStart.SnapStart();
                        if (err != ErrorCode.Success)
                            Debug.WriteLine(err, "Error starting GPI device");
                        else
                        {
                            Connected = true;
                            Debug.WriteLine(this, "StartPin SnapStart OK");
                        }
                    }
                }
                if (Type == GPIType.Remote)
                {
                    _heartbeatTimer = new Timer(new TimerCallback(_heartbeatTick), null, 0, 5000);
                    _socketWorker = new Thread(new ThreadStart(_socketWorkerThreadProc));
                    _socketWorker.Name = "GPINotifier socket thread worker";
                    _socketWorker.Priority = ThreadPriority.AboveNormal;
                    _socketWorker.IsBackground = true;
                    _socketWorker.Start();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(this, e.Message);
            }
        }

        private void RemoteConnect()
        {
            Connected = false;
            var oldStream = _remoteClientStream;
            if (oldStream != null)
                oldStream.Close();
            var oldClient = _remoteClient;
            if (oldClient != null)
                _remoteClient.Close();
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
                    _remoteClientStream.WriteByte((byte)CrawlCommand.SetIsController);
                    _remoteClientStream.WriteByte((byte)CrawlCommand.GetInfo);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "GPINotifier client connect error");
                Thread.Sleep(10000);
            }
        }

        private void _socketWorkerThreadProc()
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
                    Debug.WriteLine(e.Message, "GPINotifier client receive error");
                }
                if (received > 0)
                        _notificationReceived(buffer, received);
                else  // client disconnected
                {
                    if (!disposed)
                        RemoteConnect();
                }
            }
        }

        private void _notificationReceived(byte[] data, int bufferLength)
        {
            int bufferPos = 0;
            while (bufferLength>bufferPos)
                switch (data[bufferPos])
                {
                    case (byte)CrawlCommand.HeartBeat:
                        _hearbeatFailCount = 0;
                        Connected = true;
                        bufferPos++;
                        break;
                    case (byte)CrawlCommand.PlayoutStart:
                        var handler = StartPressed;
                        bufferPos++;
                        if (handler != null)
                        {
                            Debug.WriteLine(this, "PlayoutStart from");
                            handler();
                        }
                        break;
                    case (byte)CrawlCommand.ShowCrawl:
                        CrawlState.CrawlVisible = true;
                        bufferPos++;
                        NotifyPropertyChanged("Crawl");
                        break;
                    case (byte)CrawlCommand.HideCrawl:
                        CrawlState.CrawlVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Crawl");
                        break;
                    case (byte)CrawlCommand.SetCrawl:
                    case (byte)CrawlCommand.ReloadCrawl:
                        if (bufferLength - bufferPos >= 2)
                        {
                            CrawlState.ConfigNr = data[bufferPos + 1];
                            NotifyPropertyChanged("Crawl");
                            if (data[bufferPos] == (byte)CrawlCommand.SetCrawl)
                            {
                                CrawlState.CrawlVisible = true;
                                NotifyPropertyChanged("Crawl");
                            }
                        }
                        bufferPos += 2;
                        break;
                    case (byte)CrawlCommand.ShowLogo0:
                    case (byte)CrawlCommand.ShowLogo0 + 1:
                    case (byte)CrawlCommand.ShowLogo0 + 2:
                    case (byte)CrawlCommand.ShowLogo0 + 3:
                    case (byte)CrawlCommand.ShowLogo0 + 4:
                    case (byte)CrawlCommand.ShowLogo0 + 5:
                    case (byte)CrawlCommand.ShowLogo0 + 6:
                    case (byte)CrawlCommand.ShowLogo0 + 7:
                    case (byte)CrawlCommand.ShowLogo0 + 8:
                    case (byte)CrawlCommand.ShowLogo0 + 9:
                        CrawlState.LogoVisible = true;
                        CrawlState.LogoStyle = (byte)(data[bufferPos] - (byte)CrawlCommand.ShowLogo0);
                        bufferPos++;
                        NotifyPropertyChanged("Logo");
                        break;
                    case (byte)CrawlCommand.HideLogo:
                        CrawlState.LogoVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Logo");
                        break;
                    case (byte)CrawlCommand.ShowParental0:
                    case (byte)CrawlCommand.ShowParental0 + 1:
                    case (byte)CrawlCommand.ShowParental0 + 2:
                    case (byte)CrawlCommand.ShowParental0 + 3:
                    case (byte)CrawlCommand.ShowParental0 + 4:
                    case (byte)CrawlCommand.ShowParental0 + 5:
                    case (byte)CrawlCommand.ShowParental0 + 6:
                    case (byte)CrawlCommand.ShowParental0 + 7:
                    case (byte)CrawlCommand.ShowParental0 + 8:
                    case (byte)CrawlCommand.ShowParental0 + 9:
                        CrawlState.ParentalVisible = true;
                        CrawlState.ParentalStyle = (byte)(data[bufferPos] - (byte)CrawlCommand.ShowParental0);
                        bufferPos++;
                        NotifyPropertyChanged("Parental");
                        break;
                    case (byte)CrawlCommand.HideParental:
                        CrawlState.ParentalVisible = false;
                        bufferPos++;
                        NotifyPropertyChanged("Parental");
                        break;
                    case (byte)CrawlCommand.AspectNarrow:
                        CrawlState.AspectNarrow = true;
                        bufferPos++;
                        NotifyPropertyChanged("AspectNarrow");
                        break;
                    case (byte)CrawlCommand.AspectWide:
                        CrawlState.AspectNarrow = false;
                        bufferPos++;
                        NotifyPropertyChanged("AspectNarrow");
                        break;
                    case (byte)CrawlCommand.MasterTake:
                        _isMaster = true;
                        bufferPos++;
                        NotifyPropertyChanged("IsMaster");
                        break;
                    case (byte)CrawlCommand.MasterFree:
                        _isMaster = false;
                        bufferPos++;
                        NotifyPropertyChanged("IsMaster");
                        break;
                    case (byte)CrawlCommand.GetInfo:
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
                        stream.WriteByte((byte)CrawlCommand.HeartBeat);
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
                if (Type == GPIType.Advantech)
                {
                    if (_instantDiCtrlStart != null)
                    {
                        _instantDiCtrlStart.SnapStop();
                        _instantDiCtrlStart.Interrupt -= new EventHandler<DiSnapEventArgs>(instantDiCtrlStart_Interrupt);
                    }
                }
                if (Type == GPIType.Remote)
                {
                    var client = _remoteClient;
                    if (client != null)
                        client.Close();
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }


        void instantDiCtrlStart_Interrupt(object sender, DiSnapEventArgs e)
        {
            lock (InputPinStart)
            {

                bool newState = (e.PortData[InputPinStart.PortNumber] & InputPinStart.PinDecimal) > 0;
                if (newState)
                {
                    var handler = StartPressed;
                    if (handler != null)
                    {
                        Debug.WriteLine(this, "Starting");
                        handler();
                    }
                }
            }
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

        public bool AspectNarrow
        {
            get { return CrawlState.AspectNarrow; }
            set
            {
                if (CrawlState.AspectNarrow != value)
                {
                    if (Type == GPIType.Remote)
                    {
                        if (value)
                            _sendCommand((byte)CrawlCommand.AspectNarrow);
                        else
                            _sendCommand((byte)CrawlCommand.AspectWide);
                    }
                }
            }
        }

        public bool CrawlVisible
        {
            get { return CrawlState.CrawlVisible; }
            set
            {
                if (CrawlState.CrawlVisible != value)
                {
                    if (Type == GPIType.Remote)
                        _sendCommand(value ? (byte)CrawlCommand.ShowCrawl : (byte)CrawlCommand.HideCrawl);
                }
            }
        }

        public TCrawl Crawl
        {
            get { return CrawlState.CrawlVisible ? (TCrawl)(CrawlState.ConfigNr) : TCrawl.NoCrawl; }
            set
            {
                if (Crawl != value)
                {
                    if (Type == GPIType.Remote)
                        if (value == TCrawl.NoCrawl)
                            _sendCommand((byte)CrawlCommand.HideCrawl);
                        else
                            _sendCommand((byte)CrawlCommand.SetCrawl, (byte)value);
                }
            }
        }

        public TLogo Logo
        {
            get
            {
                if (!CrawlState.LogoVisible)
                    return TLogo.NoLogo;
                else
                    return (TLogo)(CrawlState.LogoStyle + 1);
            }
            set
            {
                if (Logo != value)
                {
                    if (Type == GPIType.Remote)
                    {
                        if (value == TLogo.NoLogo)
                            _sendCommand((byte)CrawlCommand.HideLogo);
                        else
                            _sendCommand((byte)((byte)CrawlCommand.ShowLogo0 + ((byte)(value) - 1)));
                    }
                }
            }
        }

        public TParental Parental
        {
            get
            {
                if (!CrawlState.ParentalVisible)
                    return TParental.None;
                else
                    return (TParental)(CrawlState.ParentalStyle + 1);
            }
            set
            {
                if (Parental != value)
                {
                    if (Type == GPIType.Remote)
                    {
                        if (value == TParental.None)
                            _sendCommand((byte)CrawlCommand.HideParental);
                        else
                            _sendCommand((byte)((byte)CrawlCommand.ShowParental0 + ((byte)(value) - 1)));
                    }
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

    public class InputPin
    {
        [XmlAttribute]
        public int DeviceId;
        internal byte PinDecimal { get; private set; }
        [XmlAttribute]
        public byte PinNumber 
        {
            get { return 0; }
            set { PinDecimal = (byte) (1 << value); } 
        }
        [XmlAttribute]
        public byte PortNumber;
    }
    
}
