using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar.AMCP
{
	internal class AMCPProtocolStrategy : Svt.Network.IProtocolStrategy
	{
		CasparDevice device_ = null;
		AMCPParser parser_ = new AMCPParser();

		internal AMCPProtocolStrategy(CasparDevice device)
		{
			device_ = device;
			parser_.ResponseParsed += new EventHandler<AMCPParserEventArgs>(parser__ResponseParsed);
		}

		void parser__ResponseParsed(object sender, AMCPParserEventArgs e)
		{
			//A response is completely parsed
			//Info about it is in the eventArgs
			if (e.Error == AMCPError.None)
			{
				switch (e.Command)
				{
					case AMCPCommand.VERSION:
						device_.OnVersion(e.Data[0]);
						break;
					case AMCPCommand.CLS:
						OnCLS(e);
						break;
					case AMCPCommand.TLS:
						OnTLS(e);
						break;
					case AMCPCommand.INFO:
						OnInfo(e);
						break;
					case AMCPCommand.LOAD:
						device_.OnLoad((string)((e.Data.Count > 0) ? e.Data[0] : string.Empty));
						break;
					case AMCPCommand.LOADBG:
						device_.OnLoadBG((string)((e.Data.Count > 0) ? e.Data[0] : string.Empty));
						break;
					case AMCPCommand.PLAY:
						break;
					case AMCPCommand.STOP:
						break;
					case AMCPCommand.CG:
						break;
					case AMCPCommand.CINF:
						break;
					case AMCPCommand.DATA:
						OnData(e);
						break;
				}
			}
			else
			{
				if (e.Command == AMCPCommand.DATA)
					OnData(e);
			}
		}

		private void OnData(AMCPParserEventArgs e)
		{
			if (e.Error == AMCPError.FileNotFound)
			{
				device_.OnDataRetrieved(string.Empty);
				return;
			}

			if (e.Subcommand == "RETRIEVE")
			{
				if (e.Error == AMCPError.None && e.Data.Count > 0)
					device_.OnDataRetrieved(e.Data[0]);
				else
					device_.OnDataRetrieved(string.Empty);
			}
			else if (e.Subcommand == "LIST")
			{
				device_.OnUpdatedDataList(e.Data);
			}
		}

		private void OnTLS(AMCPParserEventArgs e)
		{
			List<TemplateInfo> templates = new List<TemplateInfo>();
			foreach (string templateInfo in e.Data)
			{
				string pathName = templateInfo.Substring(templateInfo.IndexOf('\"')+1, templateInfo.IndexOf('\"', 1)-1);
				string folderName = "";
				string fileName = "";
				int delimIndex = pathName.LastIndexOf('\\');
				if (delimIndex != -1)
				{
					folderName = pathName.Substring(0, delimIndex);
					fileName = pathName.Substring(delimIndex + 1);
				}
				else {
					fileName = pathName;
				}

				string temp = templateInfo.Substring(templateInfo.LastIndexOf('\"') + 1);
				string[] sizeAndDate = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				Int64 size = Int64.Parse(sizeAndDate[0]);
				DateTime updated = DateTime.ParseExact(sizeAndDate[1], "yyyyMMddHHmmss", null);

				templates.Add(new TemplateInfo(folderName, fileName, size, updated));
			}

			device_.OnUpdatedTemplatesList(templates);
		}

		private void OnCLS(AMCPParserEventArgs e)
		{
			List<MediaInfo> clips = new List<MediaInfo>();
			foreach (string mediaInfo in e.Data)
			{
				string pathName = mediaInfo.Substring(mediaInfo.IndexOf('\"') + 1, mediaInfo.IndexOf('\"', 1) - 1);
				string folderName = "";
				string fileName = "";
				int delimIndex = pathName.LastIndexOf('\\');
				if (delimIndex != -1)
				{
					folderName = pathName.Substring(0, delimIndex);
					fileName = pathName.Substring(delimIndex + 1);
				}
				else
				{
					fileName = pathName;
				}

				string temp = mediaInfo.Substring(mediaInfo.LastIndexOf('\"') + 1);
				string[] vSizeTypeAndDate = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				MediaType type = (MediaType)Enum.Parse(typeof(MediaType), vSizeTypeAndDate[0]);
				Int64 size = Int64.Parse(vSizeTypeAndDate[1]);
				DateTime updated = DateTime.ParseExact(vSizeTypeAndDate[2], "yyyyMMddHHmmss", null);

				clips.Add(new MediaInfo(folderName, fileName, type, size, updated));
			}

			device_.OnUpdatedMediafiles(clips);
		}

		void OnInfo(AMCPParserEventArgs e)
		{
			List<ChannelInfo> channelInfo = new List<ChannelInfo>();
			foreach (string channelData in e.Data)
			{
				string[] data = channelData.Split(' ');
				int id = Int32.Parse(data[0]);
//				VideoMode vm = (VideoMode)Enum.Parse(typeof(VideoMode), data[1]);
//				ChannelStatus cs = (ChannelStatus)Enum.Parse(typeof(ChannelStatus), data[2]);
				channelInfo.Add(new ChannelInfo(id, VideoMode.Unknown, ChannelStatus.Stopped, ""));
			}

			device_.OnUpdatedChannelInfo(channelInfo);
		}

		#region IProtocolStrategy Members
		public string Delimiter
		{
			get { return AMCPParser.CommandDelimiter; }
		}

		public Encoding Encoding
		{
			get { return System.Text.Encoding.UTF8; }
		}

		public void Parse(string data, Svt.Network.RemoteHostState state)
		{
			parser_.Parse(data);
		}
		public void Parse(byte[] data, int length, Svt.Network.RemoteHostState state) { throw new NotImplementedException(); }

		#endregion
	}
}
