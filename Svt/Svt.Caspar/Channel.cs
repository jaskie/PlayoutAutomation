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
            if (item.VideoLayer == -1)
            {
                if (item.Seek == -1)
                    Connection.SendString("LOAD " + ID + " " + clipname + (string)(item.Loop ? " LOOP" : "") + " " + item.Transition);
                else
                    Connection.SendString("LOAD " + ID + " " + clipname + (string)(item.Loop ? " LOOP" : "") + " " + item.Transition + " SEEK " + item.Seek + " LENGTH " + item.Length);
            }
            else
            {
                if (item.Seek == -1)
                    Connection.SendString("LOAD " + ID + "-" + item.VideoLayer + " " + clipname + (string)(item.Loop ? " LOOP" : "") + " " + item.Transition);
                else
                    Connection.SendString("LOAD " + ID + "-" + item.VideoLayer + " " + clipname + (string)(item.Loop ? " LOOP" : "") + " " + item.Transition + " SEEK " + item.Seek + " LENGTH " + item.Length);
            }

            return true;
        }
       
        public bool LoadBG(CasparItem item)
		{
            string clipname = item.Clipname.Replace("\\", "\\\\");
            if (item.VideoLayer == -1)
            {
                if (item.Seek == -1)
                    Connection.SendString("LOADBG " + ID + " " + clipname + (string)(item.Loop ? " LOOP" : "") + (string)(item.Auto ? " AUTO" : "") + " " + item.Transition);
                else
                    Connection.SendString("LOADBG " + ID + " " + clipname + (string)(item.Loop ? " LOOP" : "") + (string)(item.Auto ? " AUTO" : "") + " " + item.Transition + " SEEK " + item.Seek + " LENGTH " + item.Length);
            }
            else
            {
                if (item.Seek == -1)
                    Connection.SendString("LOADBG " + ID + "-" + item.VideoLayer + " " + clipname + (string)(item.Loop ? " LOOP" : "") + (string)(item.Auto ? " AUTO" : "") + " " + item.Transition);
                else
                    Connection.SendString("LOADBG " + ID + "-" + item.VideoLayer + " " + clipname + (string)(item.Loop ? " LOOP" : "") + (string)(item.Auto ? " AUTO" : "") + " " + item.Transition + " SEEK " + item.Seek + " LENGTH " + item.Length);
            }

			return true;
		}
        public bool LoadBG(int videoLayer, string clipname, bool loop)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : ""));
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : ""));
           
            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : "") + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration)
		{
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
			    Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString());

			return true;
		}
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());

            return true;
        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, int seek)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek);

            return true;

        }
        public bool LoadBG(int videoLayer, string clipname, bool loop, TransitionType transition, uint transitionDuration, TransitionDirection direction, uint seek, uint length)
        {
            clipname = clipname.Replace("\\", "\\\\");
            if (videoLayer == -1)
                Connection.SendString("LOADBG " + ID + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());
            else
                Connection.SendString("LOADBG " + ID + "-" + videoLayer + " " + clipname + (string)(loop ? " LOOP" : "") + " " + transition.ToString() + " " + transitionDuration.ToString() + " " + direction.ToString() + " SEEK " + seek.ToString() + " LENGTH " + length.ToString());

            return true;
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

        public void CustomCommand(string command)
        {
            Connection.SendString(command);
        }
        
        public void ClearMixer(int videoLayer)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CLEAR", ID));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CLEAR", ID, videoLayer));
        }

        public void SetVolume(int videoLayer, float volume, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} VOLUME {1} {2} {3}", ID, volume, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} VOLUME {2} {3} {4}", ID, videoLayer, volume, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetOpacity(int videoLayer, float opacity, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} OPACITY {1} {2} {3}", ID, opacity, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} OPACITY {2} {3} {4}", ID, videoLayer, opacity, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetBrightness(int videoLayer, float brightness, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} BRIGHTNESS {1} {2} {3}", ID, brightness, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} BRIGHTNESS {2} {3} {4}", ID, videoLayer, brightness, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetContrast(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} CONTRAST {1} {2} {3}", ID, contrast, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} CONTRAST {2} {3} {4}", ID, videoLayer, contrast, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetSaturation(int videoLayer, float contrast, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} SATURATION {1} {2} {3}", ID, contrast, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} SATURATION {2} {3} {4}", ID, videoLayer, contrast, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetLevels(int videoLayer, float minIn, float maxIn, float gamma, float minOut, float maxOut, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} LEVELS {1} {2} {3} {4} {5} {6} {7}", ID, minIn, maxIn, gamma, minOut, maxOut, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} LEVELS {2} {3} {4} {5} {6} {7} {8}", ID, videoLayer, minIn, maxIn, gamma, minOut, maxOut, duration, Enum.GetName(typeof(Easing), easing)));
        }

        public void SetGeometry(int videoLayer, float x, float y, float scaleX, float scaleY, int duration, Easing easing)
        {
            if (videoLayer == -1)
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0} FILL {1} {2} {3} {4} {5} {6}", ID, x, y, scaleX, scaleY, duration, Enum.GetName(typeof(Easing), easing)));
            else
                Connection.SendString(string.Format(CultureInfo.InvariantCulture, "MIXER {0}-{1} FILL {2} {3} {4} {5} {6} {7}", ID, videoLayer, x, y, scaleX, scaleY, duration, Enum.GetName(typeof(Easing), easing)));
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
