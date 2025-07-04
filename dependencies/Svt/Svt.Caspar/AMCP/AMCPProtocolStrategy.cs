using System;
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
                    case AMCPCommand.INFO:
                        OnInfo(e);
                        break;
                    case AMCPCommand.LOAD:
                    case AMCPCommand.LOADBG:
                    case AMCPCommand.PLAY:
                    case AMCPCommand.STOP:
                    case AMCPCommand.CG:
                    case AMCPCommand.CINF:
                        break;
                    // currently only CALL command is handled
                    case AMCPCommand.CALL:
                        device_.OnCommandResponse(e.RequestId, e.Command, e.Error);
                        break;
                }
            }
        }

        void OnInfo(AMCPParserEventArgs e)
        {
            if (e.Subcommand == string.Empty)
                device_.OnUpdatedServerInfo(e.Data); //for 2.2 and newer
            else if (Enum.TryParse(e.Subcommand, out ACMPInfoKind infoKind))
                switch (infoKind)
                {
                    case ACMPInfoKind.SERVER:
                        device_.OnUpdatedChannelInfo(string.Join("\n", e.Data));
                        break;
                    case ACMPInfoKind.RECORDERS:
                        device_.OnUpdatedRecorderInfo(string.Join("\n", e.Data));
                        break;
                }
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
