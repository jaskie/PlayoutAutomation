using Svt.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Svt.Caspar
{
    public enum RecorderKind
    {
        decklink,
    }

    public enum DeckState
    {
        unknown = 0,
        not_vtr_control = 2,
        playing = 4,
        recording = 8,
        still = 0x10,
        shuttle_forward = 0x20,
        shuttle_reverse = 0x40,
        jog_forward = 0x80,
        jog_reverse = 0x100,
        stopped = 0x200
    }

    public enum DeckControl
    {
        none,
        export_prepare,
        export_complete,
        aborted,
        capture_prepare,
        capture_complete
    }

    [XmlRoot("recorders", Namespace = "")]
    public class RecorderList
    {
        [XmlElement("recorder", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public List<Recorder> Recorders { get; set; }
    }

    public class Recorder
    {
        #region Serialized
        [XmlElement("recorder-kind")]
        public RecorderKind RecorderKind { get; set; }
        [XmlElement("device")]
        public int Device { get; set; }
        [XmlElement("preroll")]
        public int Preroll { get; set; }
        [XmlElement("index")]
        public int Id { get; set; }
        private bool _isConnected;
        [XmlElement("connected")]
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    DeckConnected?.Invoke(this, new DeckConnectedEventArgs(value));
                }
            }
        }
        #endregion Serialized

        internal ServerConnection Connection { get; set; }

        public bool Play()
        {
            Connection.SendString($"RECORDER PLAY {Id}");
            return true;
        }

        public bool Stop()
        {
            Connection.SendString($"RECORDER STOP {Id}");
            return true;
        }

        public bool Abort()
        {
            Connection.SendString($"RECORDER ABORT {Id}");
            return true;
        }

        public bool FastForward()
        {
            Connection.SendString($"RECORDER FF {Id}");
            return true;
        }
        public bool Rewind()
        {
            Connection.SendString($"RECORDER REWIND {Id}");
            return true;
        }
        public bool GotoTimecode(string Timecode)
        {
            Connection.SendString($"RECORDER GOTO {Id} TC {Timecode}");
            return true;
        }

        public bool Capture(int channel, string tcIn, string tcOut, bool narrowMode, string filename)
        {
            Connection.SendString($"CAPTURE {channel} RECORDER {Id} IN {tcIn} OUT {tcOut} NARROW {narrowMode} FILE \"{filename}\"");
            return true;
        }

        public bool Capture(int channel, long frames, bool narrowMode, string filename)
        {
            Connection.SendString($"CAPTURE {channel} RECORDER {Id} LIMIT {frames} NARROW {narrowMode} FILE \"{filename}\"");
            return true;
        }

        public bool Finish()
        {
            Connection.SendString($"RECORDER FINISH {Id}");
            return true;
        }

        public bool SetTimeLimit(long frames)
        {
            Connection.SendString($"RECORDER CALL {Id} LIMIT {frames}");
            return true;
        }

        #region OSC notifications

        internal void OscMessage(Network.Osc.OscMessage message)
        {
            string[] path = message.Address.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int id;
            if (path.Length >= 3 && message.Arguments.Count == 1)
            {
                if (path[0] == "recorder" &&
                    int.TryParse(path[1], out id) && id == Id)
                {
                    switch (path[2])
                    {
                        case "tc":
                            Tc?.Invoke(this, new TcEventArgs(message.Arguments[0].ToString()));
                            break;
                        case "control":
                            DeckControl control;
                            if (Enum.TryParse(message.Arguments[0].ToString(), out control))
                                DeckControl?.Invoke(this, new DeckControlEventArgs(control));
                            break;
                        case "state":
                            DeckState state;
                            if (Enum.TryParse(message.Arguments[0].ToString(), out state))
                                DeckState?.Invoke(this, new DeckStateEventArgs(state));
                            break;
                        case "connected":
                            bool isConnected;
                            if (bool.TryParse(message.Arguments[0].ToString(), out isConnected))
                                IsConnected = isConnected;
                            break;
                        case "frames_left":
                            if (message.Arguments[0] is long)
                                FramesLeft?.Invoke(this, new FramesLeftEventArgs((long)message.Arguments[0]));
                            break;
                        default:
                            Debug.WriteLine($"Unrecognized message: {path[2]}");
                            break;
                    }
                }
            }
        }
  
        public event EventHandler<TcEventArgs> Tc;
        public event EventHandler<FramesLeftEventArgs> FramesLeft;
        public event EventHandler<DeckStateEventArgs> DeckState;
        public event EventHandler<DeckControlEventArgs> DeckControl;
        public event EventHandler<DeckConnectedEventArgs> DeckConnected;

        #endregion OSC notifications

        

    }

    public class TcEventArgs : EventArgs
    {
        public TcEventArgs(string tc)
        {
            Tc = tc;
        }
        public string Tc { get; private set; }
    }

    public class FramesLeftEventArgs: EventArgs
    {
        public FramesLeftEventArgs(long frames)
        {
            FramesLeft = frames;
        }
        public long FramesLeft { get; private set; }
    }

    public class DeckStateEventArgs: EventArgs
    {
        public DeckStateEventArgs(DeckState state)
        {
            State = state;
        }
        public DeckState State { get; private set; }
    }

    public class DeckControlEventArgs : EventArgs
    {
        public DeckControlEventArgs(DeckControl controlEvent)
        {
            ControlEvent = controlEvent;
        }
        public DeckControl ControlEvent { get; private set; }
    }

    public class DeckConnectedEventArgs : EventArgs
    {
        public DeckConnectedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
        public bool IsConnected { get; private set; }
    }
}
