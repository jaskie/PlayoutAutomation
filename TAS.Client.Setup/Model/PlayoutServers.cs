using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Database;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup.Model
{
    public class PlayoutServers
    {
        readonly string _connectionString;
        readonly string _connectionStringSecondary;
        readonly List<CasparServer> _servers;
        public PlayoutServers(string connectionString, string connectionStringSecondary)
        {
            _connectionString = connectionString;
            _connectionStringSecondary = connectionStringSecondary;
            try
            {
                Database.Open(_connectionString, _connectionStringSecondary);
                _servers = Database.DbLoadServers<CasparServer>();
                _servers.ForEach(s =>
                    {
                        s.IsNew = false;
                        s.Channels.ForEach(c => c.Owner = s);
                    });
            }
            finally
            {
                Database.Close();
            }
        }

        public void Save()
        {
            try
            {
                Database.Open(_connectionString, _connectionStringSecondary);
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
                Database.Close();
            }
        }

        public List<CasparServer> Servers { get { return _servers; } }
        public List<CasparServer> DeletedServers = new List<CasparServer>();
    }
}
