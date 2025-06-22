using System;
using System.Collections.Generic;
using System.Linq;

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
        CINF,
        VERSION,
        INFO,
        CLEAR,
        SET,
        MIXER,
        CALL,
        REMOVE,
        ADD,
        SWAP,
        STATUS,
        PAUSE,
        CAPTURE,
        RECORDER,
        Undefined
    }

    internal enum ACMPInfoKind
    {
        None,
        SERVER,
        RECORDERS,
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
        public string RequestId { get; set; } = string.Empty;
        public AMCPCommand Command { get; set; } = AMCPCommand.None;
        public string Subcommand { get; set; } = string.Empty;
        public AMCPError Error { get; set; } = AMCPError.None;
        public List<string> Data { get; } = new List<string>();
    }

    class AMCPParser
    {
        internal const string CommandDelimiter = "\r\n";

        public event EventHandler<AMCPParserEventArgs> ResponseParsed;

        AMCPParserEventArgs nextParserEventArgs_ = new AMCPParserEventArgs();

        AMCPParserState State { get; set; } = AMCPParserState.ExpectingHeader;

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
                ResponseParsed?.Invoke(this, nextParserEventArgs_);
                State = AMCPParserState.ExpectingHeader;
                nextParserEventArgs_ = new AMCPParserEventArgs();
            }
        }

        #region Parser helpers

        //Returning true indicates that a command response is completely parsed
        bool ParseHeader(string line)
        {
            if (line.Length > 0)
            {
                string[] tokens;
                // requestID is present in header
                if (line[0] == '#')
                {
                    tokens = line.Split(new [] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    
                    // check for invalid header
                    if (tokens.Length != 2)
                        return false;
                    // remove the leading '#'
                    nextParserEventArgs_.RequestId = tokens[0].Substring(1);
                    line = tokens[1]; //use the rest of the line as the actual header
                }
                tokens = line.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                // check if header is not empty
                if (tokens.Length == 0)
                    return false;
                switch (tokens[0].FirstOrDefault())
                {
                    case '1':
                        return ParseInformationalHeader(tokens);
                    case '2':
                        return ParseSuccessHeader(tokens);
                    case '4':
                        return ParseClientErrorHeader(tokens);
                    case '5':
                        return ParseServerErrorHeader(tokens);
                }
            }
            else
            {
                //oops, what to do now? That was NOT expected.
                //Just consume the line and hope the next one really IS a header
            }
            return false;
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
