using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class PlayoutServers
    {
        readonly string _connectionString;
        readonly List<CasparServer> _servers;
        public PlayoutServers(string connectionString)
        {
            this._connectionString = connectionString;
            try
            {
                Database.Initialize(connectionString);
                _servers = Database.DbLoadServers<CasparServer>();
                _servers.ForEach(s =>
                    {
                        s.IsNew = false;
                        s.Channels.ForEach(c => c.Owner = s);
                    });
            }
            finally
            {
                Database.Uninitialize();
            }
        }

        public void Save()
        {
            try
            {
                Database.Initialize(_connectionString);
                _servers.ForEach(s =>
                {
                    if (s.Id == 0)
                        s.DbInsertServer();
                    else
                        s.DbUpdateServer();
                });
                DeletedServers.ForEach(s => { if (s.Id > 0) s.DbDeleteServer(); });
            }
            finally
            {
                Database.Uninitialize();
            }
        }

        public List<CasparServer> Servers { get { return _servers; } }
        public List<CasparServer> DeletedServers = new List<CasparServer>();
    }
}
