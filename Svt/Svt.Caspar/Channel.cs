using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Svt.Caspar
{
	public class Channel
	{
		public int ID { get; private set; }
        public CGManager CG { get; private set; }
        public VideoMode VideoMode { get; internal set; }
        internal Svt.Network.ServerConnection Connection { get; private set; }

		internal Channel(Svt.Network.ServerConnection connection, int id, VideoMode videoMode)
		{
			ID = id;
            VideoMode = videoMode;
            Connection = connection;
			CG = new CGManager(this);
		}


		public bool Load(string clipname, bool loop)
		{
            clipname = clipname.Replace("\\", "\\\\");
			Connection.SendString("LOAD " + ID + " " + clipname + (string)(loop ? " LOOP" : ""));
			return true;
		}
        public bool Load(int videoLayer, string clipname, bool loop)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Load(clipname, loop);
            else
                Connection.SendString("LOAD " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : ""));

            return true;
        }
		public bool Load(CasparItem item)
		{
            string clipname = item.Clipname.Replace("\\", "\\\\");
            var command = new StringBuilder("LOAD ").Append(ID);
            if (item.VideoLayer >= 0) command.AppendFormat("-{0}", item.VideoLayer);
            command.Append(" ").Append(clipname);
            if (item.Seek > 0) command.AppendFormat(" SEEK {0}", item.Seek);
            if (item.Length > 0) command.AppendFormat(" LENGTH {0}", item.Length);
            if (item.Loop) command.Append(" LOOP");
            if (item.ChannelLayout != ChannelLayout.Default) command.AppendFormat(" CHANNEL_LAYOUT {0}", item.ChannelLayout.ToString().ToUpperInvariant());
            if (item.Transition != null)
                command.AppendFormat(" {0}", item.Transition);
            if (item.FieldOrderInverted)
                command.Append(" FIELD_ORDER_INVERTED");
            Connection.SendString(command.ToString());
            return true;
        }
       
        public bool LoadBG(CasparItem item)
		{
            string clipname = item.Clipname.Replace("\\", "\\\\");
            var command = new StringBuilder("LOADBG ").Append(ID);
            if (item.VideoLayer >= 0) command.AppendFormat("-{0}", item.VideoLayer);
            command.Append(" ").Append(clipname);
            if (item.Seek > 0) command.AppendFormat(" SEEK {0}", item.Seek);
            if (item.Length > 0) command.AppendFormat(" LENGTH {0}", item.Length);
            if (item.Loop) command.Append(" LOOP");
            if (item.ChannelLayout != ChannelLayout.Default) command.AppendFormat(" CHANNEL_LAYOUT {0}", item.ChannelLayout.ToString().ToUpperInvariant());
            if (item.Transition != null)
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
                Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : ""));
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : ""));
           
            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration)
		{
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
			    Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());

			return true;
		}
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, int seek)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek);

            return true;

        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " \"" + clipname + "\"" + (loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
        }

		public void Pause()
		{
			Connection.SendString("PAUSE " + ID);
		}

        public void Pause(int videoLayer)
        {
            if (videoLayer == -1)
                Pause();
            else
                Connection.SendString("PAUSE " + ID + "-" + videoLayer);
        }

        public void Play()
        {
            Connection.SendString("PLAY " + ID);
        }
        public void Play(int videoLayer)
        {
            if (videoLayer == -1)
                Play();
            else
                Connection.SendString("PLAY " + ID + "-" + videoLayer);
        }

        public void Stop()
		{
			Connection.SendString("STOP " + ID);
		}

        public void Stop(int videoLayer)
        {
            if (videoLayer == -1)
                Stop();
            else
                Connection.SendString("STOP " + ID + "-" + videoLayer);
        }

        public void Seek(int videoLayer, uint seek)
        {
            Connection.SendString(string.Format("CALL {0}-{1} SEEK {2}", ID, videoLayer, seek));
        }

        public void SetInvertedFieldOrder(int videoLayer, bool invert)
        {
            Connection.SendString(string.Format("CALL {0}-{1} FIELD_ORDER_INVERTED {2}", ID, videoLayer, invert ? 1 : 0));
        }

		public void Clear()
		{
			Connection.SendString("CLEAR " + ID);
		}
        public void Clear(int videoLayer)
        {
            if (videoLayer == -1)
                Clear();
            else
                Connection.SendString("CLEAR " + ID + "-" + videoLayer);
        }

		public void SetMode(VideoMode mode)
		{
			Connection.SendString("SET " + ID + " MODE " + ToAMCPString(mode));
		}

        public void ClearMixer()
        {
            ClearMixer(-1);
        }

        public void ClearMixer(int videoLayer)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CLEAR", ID));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CLEAR", ID, videoLayer));
        }

        public void SetVolume(float volume, int duration, Easing easing)
        {
            SetVolume(-1, volume, duration, easing);
        }

        public void SetVolume(int videoLayer, float volume, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} VOLUME {1} {2} {3}", ID, volume, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} VOLUME {2} {3} {4}", ID, videoLayer, volume, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetMasterVolume(float volume)
        {
            Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} MASTERVOLUME {1:F3}", ID, volume));
        }

        public void SetOpacity(float opacity, int duration, Easing easing)
        {
            SetOpacity(-1, opacity, duration, easing);
        }

        public void SetOpacity(int videoLayer, float opacity, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} OPACITY {1} {2} {3}", ID, opacity, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} OPACITY {2} {3} {4}", ID, videoLayer, opacity, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetBrightness(int videoLayer, float brightness, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} BRIGHTNESS {1} {2} {3}", ID, brightness, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} BRIGHTNESS {2} {3} {4}", ID, videoLayer, brightness, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetContrast(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CONTRAST {1} {2} {3}", ID, contrast, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CONTRAST {2} {3} {4}", ID, videoLayer, contrast, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetSaturation(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} SATURATION {1} {2} {3}", ID, contrast, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} SATURATION {2} {3} {4}", ID, videoLayer, contrast, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetLevels(int videoLayer, float minIn, float maxIn, float gamma, float minOut, float maxOut, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} LEVELS {1} {2} {3} {4} {5} {6} {7}", ID, minIn, maxIn, gamma, minOut, maxOut, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} LEVELS {2} {3} {4} {5} {6} {7} {8}", ID, videoLayer, minIn, maxIn, gamma, minOut, maxOut, duration, easing.ToString().ToUpperInvariant()));
        }

        public void SetGeometry(int videoLayer, float x, float y, float scaleX, float scaleY, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} FILL {1} {2} {3} {4} {5} {6}", ID, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} FILL {2} {3} {4} {5} {6} {7}", ID, videoLayer, x, y, scaleX, scaleY, duration, easing.ToString().ToUpperInvariant()));
        }






		private string ToAMCPString(VideoMode mode)
		{
			string result = string.Empty;
			switch (mode)
			{
				case VideoMode.Unknown:
				case VideoMode.PAL:
				case VideoMode.NTSC:
					result = mode.ToString();
					break;

				default:
					{
						string modestr = mode.ToString();
						result = (modestr.Length > 2) ? modestr.Substring(2) : modestr;
						break;
					}
			}

			return result;
		}
	}

	public enum VideoMode
	{
		PAL,
		NTSC,
		SD576p2500,
		HD720p5000,
		HD1080i5000,
		Unknown
	}
	public enum ChannelStatus
	{
		Playing,
		Stopped
	}
	internal class ChannelInfo
	{
		public ChannelInfo(int id, VideoMode vm, ChannelStatus cs, string activeClip)
		{
			ID = id;
			VideoMode = vm;
			Status = cs;
			ActiveClip = activeClip;
		}

        public int ID { get; set; }
        public VideoMode VideoMode { get; set; }
        public ChannelStatus Status { get; set; }
        public string ActiveClip { get; set; }
	}
}
