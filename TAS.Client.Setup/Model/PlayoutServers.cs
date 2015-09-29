using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;

namespace TAS.Client.Setup.Model
{
    public class PlayoutServers
    {
        readonly string _connectionString;
        readonly List<PlayoutServer> _playoutServerList;
        public PlayoutServers(string connectionString)
        {
            this._connectionString = connectionString;
            try
            {
                Database.Initialize(connectionString);
                _playoutServerList = Database.DbLoadServers<PlayoutServer>();
            }
            finally
            {
                Database.Uninitize();
            }
        }

        public IList<PlayoutServer> PlayoutServerList { get { return _playoutServerList; } }
    }
}
