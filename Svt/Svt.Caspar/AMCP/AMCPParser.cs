using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar.AMCP
{
	enum AMCPParserState
	{
		ExpectingHeader,
		ExpectingOneLineData,
		ExpectingMultilineData
	}

	internal enum AMCPCommand
	{
		None,
		LOAD,
		LOADBG,
		PLAY,
		STOP,
		CG,
		CLS,
		CINF,
		VERSION,
		TLS,
		INFO,
		DATA,
		CLEAR,
		SET,
        MIXER,
        CALL,
        REMOVE,
        ADD,
        SWAP,
        STATUS,
        Undefined
	}
	internal enum AMCPError
	{
		None,
		InvalidCommand,
		InvalidChannel,
		MissingParameter,
		InvalidParameter,
		FileNotFound,
		InternalServerError,
		InvalidFile,
		UndefinedError
	}

	internal class AMCPParserEventArgs : EventArgs
	{
		public AMCPParserEventArgs()
		{
			Subcommand = string.Empty;
		}

		private AMCPCommand command_ = AMCPCommand.None;
		public AMCPCommand Command
		{
			get { return command_; }
			set { command_ = value; }
		}

		public string Subcommand { get; set; }

		private AMCPError error_ = AMCPError.None;
		public AMCPError Error
		{
			get { return error_; }
			set { error_ = value; }
		}

		private Channel channel_ = null;
		public Channel Channel
		{
			get { return channel_; }
			set { channel_ = value; }
		}	
	
		private List<string> data_ = new List<string>();
		public List<string> Data
		{
			get { return data_; }
		}	
	}

	class AMCPParser
	{
		internal const string CommandDelimiter = "\r\n";

		public event EventHandler<AMCPParserEventArgs> ResponseParsed;

		AMCPParserState currentState_ = AMCPParserState.ExpectingHeader;
		AMCPParserEventArgs nextParserEventArgs_ = new AMCPParserEventArgs();
		public AMCPParser()
		{}

		AMCPParserState State
		{
			get { return currentState_; }
			set { currentState_ = value; }
		}

		string currentResponseLine_ = "";
		internal void Parse(string data)
		{
			currentResponseLine_ += data;

			int delimiterPosition = -1;
			while ((delimiterPosition = currentResponseLine_.IndexOf(CommandDelimiter)) != -1)
			{
				string line = currentResponseLine_.Substring(0, delimiterPosition);
				ParseLine(line);
				currentResponseLine_ = currentResponseLine_.Substring(delimiterPosition + CommandDelimiter.Length);
			}
		}

		private void ParseLine(string line)
		{
			bool bCompleteResponse = false;
			switch (State)
			{
				case AMCPParserState.ExpectingHeader:
					bCompleteResponse = ParseHeader(line);
					break;

				case AMCPParserState.ExpectingOneLineData:
					bCompleteResponse = ParseOneLineData(line);
					break;

				case AMCPParserState.ExpectingMultilineData:
					bCompleteResponse = ParseMultilineData(line);
					break;
			}

			if (bCompleteResponse)
			{
				OnResponseParsed(nextParserEventArgs_);

				State = AMCPParserState.ExpectingHeader;
				nextParserEventArgs_ = new AMCPParserEventArgs();
			}
		}

		#region Parser helpers
		//Returning true indicates that a command response is completely parsed

		bool ParseHeader(string line)
		{
			string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (line.Length > 0)
			{
				switch (line[0])
				{
					case '1':
						return ParseInformationalHeader(tokens);
					case '2':
						return ParseSuccessHeader(tokens);
					case '4':
						return ParseClientErrorHeader(tokens);
					case '5':
						return ParseServerErrorHeader(tokens);

					default:
						//oops, what to do now? That was NOT expected.
						//Just consume the line and hope the next one really IS a header
						return HackParseRetrieveData(line);
				}
			}
			else
			{
				//oops, what to do now? That was NOT expected.
				//Just consume the line and hope the next one really IS a header
			}
			return false;
		}

		private bool HackParseRetrieveData(string line)
		{
			nextParserEventArgs_.Command = AMCPCommand.DATA;
			nextParserEventArgs_.Subcommand = "RETRIEVE";
			nextParserEventArgs_.Data.Add(line.Replace("\\n", "\n"));
			return true;
		}

		bool ParseSuccessHeader(string[] tokens)
		{
            if (tokens.Length >= 3)
            {
                try
                {
                    nextParserEventArgs_.Command = (AMCPCommand)Enum.Parse(typeof(AMCPCommand), tokens[1]);
                }
                catch 
                {
                    nextParserEventArgs_.Command = AMCPCommand.Undefined;
                }
            }

			if (tokens.Length >= 4)
				nextParserEventArgs_.Subcommand = tokens[2];

			if (tokens[0] == "202")
			{
				return true;
			}
			else if (tokens[0] == "201")
			{
				State = AMCPParserState.ExpectingOneLineData;
			}
			else if (tokens[0] == "200")
			{
				State = AMCPParserState.ExpectingMultilineData;
			}
			return false;
		}

		bool ParseClientErrorHeader(string[] tokens)
		{
			nextParserEventArgs_.Error = ErrorFactory(tokens[0]);

			if (tokens.Length >= 3)
            {
                try
                {
                    nextParserEventArgs_.Command = (AMCPCommand)Enum.Parse(typeof(AMCPCommand), tokens[1]);
                }
                catch
                {
                    nextParserEventArgs_.Command = AMCPCommand.Undefined;
                }
            }

			return true;
		}

		bool ParseServerErrorHeader(string[] tokens)
		{
			nextParserEventArgs_.Error = ErrorFactory(tokens[0]);

			if (tokens.Length >= 3)
            {
                try
                {
                    nextParserEventArgs_.Command = (AMCPCommand)Enum.Parse(typeof(AMCPCommand), tokens[1]);
                }
                catch
                {
                    nextParserEventArgs_.Command = AMCPCommand.Undefined;
                }
            }

			return true;
		}

		bool ParseInformationalHeader(string[] tokens)
		{
			if (tokens[0] == "100")
			{
				return true;
			}
			else if (tokens[0] == "101")
			{
				State = AMCPParserState.ExpectingOneLineData;
			}
			return false;
		}

		bool ParseOneLineData(string line)
		{
			nextParserEventArgs_.Data.Add(line);
			return true;
		}

		bool ParseMultilineData(string line)
		{
			if (line.Length == 0)
				return true;
			else
				nextParserEventArgs_.Data.Add(line);

			return false;
		}
		#endregion

		void OnResponseParsed(AMCPParserEventArgs args)
		{
			if (ResponseParsed != null)
				ResponseParsed(this, args);
		}

		AMCPError ErrorFactory(string errorCode)
		{
			switch (errorCode)
			{
				case "400":
					return AMCPError.InvalidCommand;
				case "401":
					return AMCPError.InvalidChannel;
				case "402":
					return AMCPError.MissingParameter;
				case "403":
					return AMCPError.InvalidParameter;
				case "404":
					return AMCPError.FileNotFound;
				case "500":
					return AMCPError.InternalServerError;
				case "501":
					return AMCPError.InternalServerError;
				case "502":
					return AMCPError.InvalidFile;
			}

			return AMCPError.UndefinedError;
		}
	}
}
