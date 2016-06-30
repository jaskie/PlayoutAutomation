using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Svt.Caspar
{
	public class CasparItem : System.Xml.Serialization.IXmlSerializable
	{
		public CasparItem(string clipname)
		{
			clipname_ = clipname;
		}
        public CasparItem(int videoLayer, string clipname)
        {
            videoLayer_ = videoLayer;
            clipname_ = clipname;
        }
		public CasparItem(string clipname, Transition transition)
		{
			clipname_ = clipname;
			if (transition != null)
			{
				transition_.Type = transition.Type;
				transition_.Duration = transition.Duration;
			}
		}
        public CasparItem(int videoLayer, string clipname, Transition transition)
        {
            videoLayer_ = videoLayer;
            clipname_ = clipname;
            if (transition != null)
            {
                transition_.Type = transition.Type;
                transition_.Duration = transition.Duration;
            }
        }

		public static CasparItem Create(System.Xml.XmlReader reader)
		{
			CasparItem item = new CasparItem();
			item.ReadXml(reader);
			return item;
		}
		private CasparItem()
		{}

		private string clipname_;
		public string Clipname
		{
			get { return clipname_; }
			set { clipname_ = value; }
		}

		private bool loop_ = false;
		public bool Loop
		{
			get { return loop_; }
			set { loop_ = value; }
		}
        private bool auto_ = false;
        public bool Auto
        {
            get { return auto_; }
            set { auto_ = value; }
        }

        private bool fieldOrderInverted_ = false;
        public bool FieldOrderInverted
        {
            get { return fieldOrderInverted_; }
            set { fieldOrderInverted_ = value; }
        }
        
        private int videoLayer_ = -1;
        public int VideoLayer
        {
            get { return videoLayer_; }
            set { videoLayer_ = value; }
        }

        private int seek_ = -1;
        public int Seek
        {
            get { return seek_; }
            set { seek_ = value; }
        }

        private int length_ = -1;
        public int Length
        {
            get { return length_; }
            set { length_ = value; }
        }

        private ChannelLayout _channelLayout;
        public ChannelLayout ChannelLayout
        {
            get { return _channelLayout; }
            set { _channelLayout = value; }
        }        

		private Transition transition_ = new Transition();
		public Transition Transition
		{
			get { return transition_; }
		}

		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			reader.MoveToContent();
			string clipname = reader["clipname"];
			if (!string.IsNullOrEmpty(clipname))
				Clipname = clipname;
			else
				Clipname = "";

            string videoLayer = reader["videoLayer"];
            if (!string.IsNullOrEmpty(videoLayer))
                VideoLayer = Int32.Parse(videoLayer);

            string channelLayout = reader["channel_lyout"];
            if (!string.IsNullOrEmpty(channelLayout))
                ChannelLayout = (ChannelLayout)Enum.Parse(typeof(ChannelLayout), channelLayout);

            string seek = reader["seek"];
            if (!string.IsNullOrEmpty(seek))
                Seek = Int32.Parse(seek);

            string length = reader["length"];
            if (!string.IsNullOrEmpty(length))
                Length = Int32.Parse(length);

			string loop = reader["loop"];
			bool bLoop = false;
			bool.TryParse(loop, out bLoop);
			Loop = bLoop;

			reader.ReadStartElement();
			if (reader.Name == "transition")
			{
				int duration = 0;

				string typeString = reader["type"];
				string durationString = reader["duration"];
				if (Int32.TryParse(durationString, out duration) && Enum.IsDefined(typeof(TransitionType), typeString.ToUpper()))
				{
					transition_ = new Transition((TransitionType)Enum.Parse(typeof(TransitionType), typeString.ToUpper()), duration);
				}
				else
					transition_ = new Transition();
			}
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("item", Properties.Resources.CasparPlayoutSchemaURL);
			writer.WriteAttributeString("clipname", Clipname);
            writer.WriteAttributeString("videoLayer", VideoLayer.ToString());
            writer.WriteAttributeString("seek", Seek.ToString());
            writer.WriteAttributeString("length", Length.ToString());
			writer.WriteAttributeString("loop", Loop.ToString());
            writer.WriteAttributeString("channel_layout", ChannelLayout.ToString());
			writer.WriteStartElement("transition");
			writer.WriteAttributeString("type", Transition.Type.ToString());
			writer.WriteAttributeString("duration", Transition.Duration.ToString());
			writer.WriteEndElement();
	
			writer.WriteEndElement();
		}
		#endregion
	}
}
