using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Linq;

namespace Svt.Caspar
{
    [XmlRoot("channels", Namespace = "")]
    public class ChannelList
    {
        [XmlElement("channel", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public Channel[] Channels { get; set; }
    }
    
    [XmlRoot("xmlElementName")]
	public class Channel: System.Xml.Serialization.IXmlSerializable
	{
        const string xmlElementName = "channel";

        private AudioData _currentAudioData;
		public int Id { get; private  set ;}
        public CGManager CG { get; private set; }
        public VideoMode VideoMode { get; internal set; }
        internal Svt.Network.ServerConnection Connection { get; set; }

		internal Channel()
		{
			CG = new CGManager(this);
		}

	    internal Channel(int id, VideoMode videoMode) : this()
	    {
	        Id = id;
	        VideoMode = videoMode;
	    }

        #region Commands

        public bool Load(string clipname, bool loop)
		{
            clipname = clipname.Replace("\\", "\\\\");
			Connection.SendString("LOAD " + Id + " " + clipname + (string)(loop ? " LOOP" : ""));
			return true;
		}
        public bool Load(int videoLayer, string clipname, bool loop)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Load(clipname, loop);
            else
                Connection.SendString("LOAD " + Id + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : ""));
            return true;
        }
		public bool Load(CasparItem item)
		{
            string clipname = item.Clipname.Replace("\\", "\\\\");
            var command = new StringBuilder("LOAD ").Append(Id);
            if (item.VideoLayer >= 0) command.AppendFormat("-{0}", item.VideoLayer);
            command.Append(" ").Append(clipname);
            if (item.Seek > 0) command.AppendFormat(" SEEK {0}", item.Seek);
            if (item.Length > 0) command.AppendFormat(" LENGTH {0}", item.Length);
            if (item.Loop) command.Append(" LOOP");
            if (item.ChannelLayout != ChannelLayout.Default) command.AppendFormat(" CHANNEL_LAYOUT {0}", item.ChannelLayout.ToString().ToUpperInvariant());
            if (item.FieldOrderInverted)
                command.Append(" FIELD_ORDER_INVERTED");
            Connection.SendString(command.ToString());
            return true;
        }
       
        public bool LoadBG(CasparItem item)
		{
            string clipname = item.Clipname.Replace("\\", "\\\\");
            var command = new StringBuilder("LOADBG ").Append(Id);
            if (item.VideoLayer >= 0) command.AppendFormat("-{0}", item.VideoLayer);
            command.Append(" ").Append(clipname);
            if (item.Seek > 0) command.AppendFormat(" SEEK {0}", item.Seek);
            if (item.Length > 0) command.AppendFormat(" LENGTH {0}", item.Length);
            if (item.Loop) command.Append(" LOOP");
            if (item.ChannelLayout != ChannelLayout.Default) command.AppendFormat(" CHANNEL_LAYOUT {0}", item.ChannelLayout.ToString().ToUpperInvariant());
            var transition = item.Transition?.Type;
            if (transition != null && transition != TransitionType.CUT)
                command.AppendFormat(" {0}", item.Transition);
            if (item.FieldOrderInverted)
                command.Append(" FIELD_ORDER_INVERTED");
            Connection.SendString(command.ToString());
            return true;
        }

        public bool LoadBG(int videoLayer, string clipname, bool loop)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : ""));
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : ""));
            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration)
		{
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
			    Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());

			return true;
		}
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, int seek)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek);

            return true;

        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + Id + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + Id + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
        }

		public void Pause()
		{
			Connection.SendString("PAUSE " + Id);
		}

        public void Pause(int videoLayer)
        {
            if (videoLayer == -1)
                Pause();
            else
                Connection.SendString("PAUSE " + Id + "-" + videoLayer);
        }

        public void Call(string function)
        {
            Connection.SendString("CALL " + Id + " " + function);
        }

        public void Call(int videoLayer, string function)
        {
            Connection.SendString("CALL " + Id + "-" + videoLayer + " " + function);
        }

        public void Play()
        {
            Connection.SendString("PLAY " + Id);
        }
        public void Play(int videoLayer)
        {
            if (videoLayer == -1)
                Play();
            else
                Connection.SendString("PLAY " + Id + "-" + videoLayer);
        }

        public void Stop()
		{
			Connection.SendString("STOP " + Id);
		}

        public void Stop(int videoLayer)
        {
            if (videoLayer == -1)
                Stop();
            else
                Connection.SendString("STOP " + Id + "-" + videoLayer);
        }

        public void Seek(int videoLayer, uint seek)
        {
            Connection.SendString(string.Format("CALL {0}-{1} SEEK {2}", Id, videoLayer, seek));
        }

        public void SetInvertedFieldOrder(int videoLayer, bool invert)
        {
            Connection.SendString(string.Format("CALL {0}-{1} FIELD_ORDER_INVERTED {2}", Id, videoLayer, invert ? 1 : 0));
        }

		public void Clear()
		{
			Connection.SendString("CLEAR " + Id);
		}
        public void Clear(int videoLayer)
        {
            if (videoLayer == -1)
                Clear();
            else
                Connection.SendString("CLEAR " + Id + "-" + videoLayer);
        }

		public void Mode(VideoMode mode)
		{
			Connection.SendString("SET " + Id + " MODE " + ToAMCPString(mode));
		}

        public void ClearMixer()
        {
            ClearMixer(-1);
        }

        public void ClearMixer(int videoLayer)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CLEAR", Id));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CLEAR", Id, videoLayer));
        }

        public void Volume(float volume, int duration, Easing easing)
        {
            Volume(-1, volume, duration, easing);
        }

        public void Volume(int videoLayer, float volume, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} VOLUME {1} {2} {3}", Id, volume, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} VOLUME {2} {3} {4}", Id, videoLayer, volume, duration, easing.ToString().ToUpperInvariant()));
        }

        public void MasterVolume(float volume)
        {
            Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} MASTERVOLUME {1:F3}", Id, volume));
        }

        public void Opacity(float opacity, int duration, Easing easing)
        {
            Opacity(-1, opacity, duration, easing);
        }

        public void Opacity(int videoLayer, float opacity, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} OPACITY {1} {2} {3}", Id, opacity, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} OPACITY {2} {3} {4}", Id, videoLayer, opacity, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Brightness(int videoLayer, float brightness, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} BRIGHTNESS {1} {2} {3}", Id, brightness, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} BRIGHTNESS {2} {3} {4}", Id, videoLayer, brightness, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Contrast(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CONTRAST {1} {2} {3}", Id, contrast, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CONTRAST {2} {3} {4}", Id, videoLayer, contrast, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Saturation(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} SATURATION {1} {2} {3}", Id, contrast, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} SATURATION {2} {3} {4}", Id, videoLayer, contrast, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Levels(int videoLayer, float minIn, float maxIn, float gamma, float minOut, float maxOut, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} LEVELS {1} {2} {3} {4} {5} {6} {7}", Id, minIn, maxIn, gamma, minOut, maxOut, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} LEVELS {2} {3} {4} {5} {6} {7} {8}", Id, videoLayer, minIn, maxIn, gamma, minOut, maxOut, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Fill(int videoLayer, float x, float y, float scaleX, float scaleY, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} FILL {1} {2} {3} {4} {5} {6}", Id, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} FILL {2} {3} {4} {5} {6} {7}", Id, videoLayer, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
        }

        public void Clip(int videoLayer, float x, float y, float scaleX, float scaleY, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CLIP {1} {2} {3} {4} {5} {6}", Id, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CLIP {2} {3} {4} {5} {6} {7}", Id, videoLayer, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
        }
        #endregion //Commands

        public event EventHandler<AudioDataEventArgs> AudioDataReceived;
        #region OSC
        internal void OscMessage(string[] address, List<object> arguments)
        {
            if (address.Length >= 3 && arguments.Count == 1)
            {
                switch (address[2])
                {
                    case "mixer":
                        if (address.Length >= 5)
                        {
                            if (address[3] == "audio")
                            {
                                if (address[4] == "nb_channels" && arguments[0] is long)
                                    NotifyAudio((int)(long)arguments[0]);
                                else
                                {
                                    int audioChannel;
                                    if (address.Length == 6
                                        && arguments[0] is float
                                        && int.TryParse(address[4], out audioChannel)
                                        && audioChannel-- > 0)
                                    {
                                        var ad = _currentAudioData;
                                        if (ad != null)
                                            switch (address[5])
                                            {
                                                case "dBFS":
                                                    if (ad.NumChannels > audioChannel)
                                                        ad.dBFS[audioChannel] = (float)arguments[0];
                                                    break;
                                                case "pFS":
                                                    if (ad.NumChannels > audioChannel)
                                                        ad.pFS[audioChannel] = (float)arguments[0];
                                                    break;
                                            }
                                    }
                                }
                                break;
                            }
                        }
                        Debug.WriteLine($"Unrecognized message: {string.Join("/", address)}:{string.Join(",", arguments)}");
                        break;
                    case "stage":
                    case "output":
                    default:
                        //Debug.WriteLine($"Unrecognized message: {string.Join("/", address)}:{string.Join(",", arguments)}");
                        break;
                }
            }
        }

        private void NotifyAudio(int numChannels)
        {
            var audioData = _currentAudioData;
            if (audioData != null && (audioData.dBFS.Any(v => v != null) || audioData.pFS.Any(v => v != null)))
                AudioDataReceived?.Invoke(this, new AudioDataEventArgs(audioData));
            _currentAudioData = new AudioData(numChannels);
        }
        #endregion OSC

       
        private string ToAMCPString(VideoMode mode)
        {
            string modestr = mode.ToString();
            return (modestr.Length > 1) ? modestr.Substring(1) : modestr;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();
            while (!(reader.Name == xmlElementName && reader.NodeType == XmlNodeType.EndElement))
            {
                if (reader.NodeType == XmlNodeType.Element)
                    switch (reader.Name)
                    {
                        case "video-mode":
                            string value = "m" + reader.ReadElementContentAsString();
                            if (Enum.TryParse(value, out VideoMode mode))
                                VideoMode = mode;
                            break;
                        case "index":
                            Id = reader.ReadElementContentAsInt();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
            }
            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public enum VideoMode
    {
        mPAL,
        mNTSC,
        m576p2500,
        m720p2398,
        m720p2400,
        m720p2500,
        m720p5000,
        m720p2997,
        m720p5994,
        m720p3000,
        m720p6000,
        m1080p2398,
        m1080p2400,
        m1080i5000,
        m1080i5994,
        m1080i6000,
        m1080p2500,
        m1080p2997,
        m1080p3000,
        m1080p5000,
        m1080p5994,
        m1080p6000,
        m1556p2398,
        m1556p2400,
        m1556p2500,
        m2160p2398,
        m2160p2400,
        m2160p2500,
        m2160p2997,
        m2160p3000,
        m2160p5000,
        Unknown
    }

    public class AudioData
    {
        internal AudioData(int numChannels)
        {
            NumChannels = numChannels;
            dBFS = new float?[numChannels];
            pFS =  new float?[numChannels];
        }
        public float?[] dBFS;
        public float?[] pFS;
        public readonly int NumChannels;
        public override string ToString()
        {
            return $"Audio data: {NumChannels}, dBFS:[{string.Join(", ", dBFS)}], pFS:[{string.Join(", ", pFS)}]";
        }
    }

    public class AudioDataEventArgs: EventArgs
    {
        internal AudioDataEventArgs(AudioData audioData)
        {
            AudioData = audioData;
        }
        public AudioData AudioData { get; private set; }
    }

}
