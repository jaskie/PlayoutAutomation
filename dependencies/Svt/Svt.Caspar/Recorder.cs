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
        public Recorder[] Recorders { get; set; } 
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
            Connection.SendRequest($"RECORDER PLAY {Id}");
            return true;
        }

        public bool Stop()
        {
            Connection.SendRequest($"RECORDER STOP {Id}");
            return true;
        }

        public bool Abort()
        {
            Connection.SendRequest($"RECORDER ABORT {Id}");
            return true;
        }

        public bool FastForward()
        {
            Connection.SendRequest($"RECORDER FF {Id}");
            return true;
        }
        public bool Rewind()
        {
            Connection.SendRequest($"RECORDER REWIND {Id}");
            return true;
        }
        public bool GotoTimecode(string timecode)
        {
            Connection.SendRequest($"RECORDER GOTO {Id} TC {timecode}");
            return true;
        }

        public bool Capture(int channel, string tcIn, string tcOut, bool narrowMode, string filename, int[] channelMap)
        {
            Connection.SendRequest(channelMap == null
                ? $"CAPTURE {channel} RECORDER {Id} IN {tcIn} OUT {tcOut} NARROW {narrowMode} FILE \"{filename}\""
                : $"CAPTURE {channel} RECORDER {Id} IN {tcIn} OUT {tcOut} NARROW {narrowMode} FILE \"{filename}\" CHANNEL_MAP \"{string.Join(",", channelMap)}\"");
            return true;
        }

        public bool Capture(int channel, long frames, bool narrowMode, string filename, int[] channelMap)
        {
            Connection.SendRequest(channelMap == null
                ? $"CAPTURE {channel} RECORDER {Id} LIMIT {frames} NARROW {narrowMode} FILE \"{filename}\""
                : $"CAPTURE {channel} RECORDER {Id} LIMIT {frames} NARROW {narrowMode} FILE \"{filename}\" CHANNEL_MAP \"{string.Join(",", channelMap)}\"");
            return true;
        }

        public bool Finish()
        {
            Connection.SendRequest($"RECORDER FINISH {Id}");
            return true;
        }

        public bool SetTimeLimit(long frames)
        {
            Connection.SendRequest($"RECORDER CALL {Id} LIMIT {frames}");
            return true;
        }

        #region OSC notifications

        internal void OscMessage(string[] address, object[] arguments)
        {
            if (address.Length >= 3 && arguments.Length == 1)
            {
                switch (address[2])
                {
                    case "tc":
                        Tc?.Invoke(this, new TcEventArgs(arguments[0].ToString()));
                        break;
                    case "control":
                        DeckControl control;
                        if (Enum.TryParse(arguments[0].ToString(), out control))
                            DeckControl?.Invoke(this, new DeckControlEventArgs(control));
                        break;
                    case "state":
                        DeckState state;
                        if (Enum.TryParse(arguments[0].ToString(), out state))
                            DeckState?.Invoke(this, new DeckStateEventArgs(state));
                        break;
                    case "connected":
                        bool isConnected;
                        if (bool.TryParse(arguments[0].ToString(), out isConnected))
                            IsConnected = isConnected;
                        break;
                    case "frames_left":
                        if (arguments[0] is long)
                            FramesLeft?.Invoke(this, new FramesLeftEventArgs((long)arguments[0]));
                        break;
                    default:
                        Debug.WriteLine($"Unrecognized message: {string.Join("/", address)}:{string.Join(",", arguments)}");
                        break;
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
        public string Tc { get; }
    }

    public class FramesLeftEventArgs: EventArgs
    {
        public FramesLeftEventArgs(long frames)
        {
            FramesLeft = frames;
        }
        public long FramesLeft { get; }
    }

    public class DeckStateEventArgs: EventArgs
    {
        public DeckStateEventArgs(DeckState state)
        {
            State = state;
        }
        public DeckState State { get; }
    }

    public class DeckControlEventArgs : EventArgs
    {
        public DeckControlEventArgs(DeckControl controlEvent)
        {
            ControlEvent = controlEvent;
        }
        public DeckControl ControlEvent { get; }
    }

    public class DeckConnectedEventArgs : EventArgs
    {
        public DeckConnectedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }
        public bool IsConnected { get; }
    }
}
