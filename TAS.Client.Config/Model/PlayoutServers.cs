using System.Collections.Generic;
using TAS.Database;

namespace TAS.Client.Config.Model
{
    public class PlayoutServers
    {
        readonly string _connectionString;
        readonly string _connectionStringSecondary;
        public PlayoutServers(string connectionString, string connectionStringSecondary)
        {
            _connectionString = connectionString;
            _connectionStringSecondary = connectionStringSecondary;
            try
            {
                Db.Open(_connectionString, _connectionStringSecondary);
                Servers = Db.DbLoadServers<CasparServer>();
                Servers.ForEach(s =>
                    {
                        s.IsNew = false;
                        s.Channels.ForEach(c => c.Owner = s);
                        s.Recorders.ForEach(r => r.Owner = s);
                    });
            }
            finally
            {
                Db.Close();
            }
        }

        public void Save()
        {
            try
            {
                Db.Open(_connectionString, _connectionStringSecondary);
                Servers.ForEach(s =>
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
                Db.Close();
            }
        }

        public List<CasparServer> Servers { get; }
        public List<CasparServer> DeletedServers = new List<CasparServer>();
    }
}
