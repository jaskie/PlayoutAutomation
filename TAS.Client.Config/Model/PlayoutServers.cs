using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class PlayoutServers
    {
        readonly IDatabase _db;
        public PlayoutServers(string connectionStringPrimary, string connectionStringSecondary)
        {
            _db = DatabaseProviderLoader.LoadDatabaseProvider();
            _db.Open(connectionStringPrimary, connectionStringSecondary);
            Servers = _db.DbLoadServers<CasparServer>();
            Servers.ForEach(s =>
                {
                    s.IsNew = false;
                    s.Channels.ForEach(c => c.Owner = s);
                    s.Recorders.ForEach(r => r.Owner = s);
                });
        }

        public void Save()
        {
            Servers.ForEach(s =>
            {
                if (s.Id == 0)
                    _db.DbInsertServer(s);
                else
                    _db.DbUpdateServer(s);
            });
            DeletedServers.ForEach(s => { if (s.Id > 0) _db.DbDeleteServer(s); });
        }

        public List<CasparServer> Servers { get; }
        public List<CasparServer> DeletedServers = new List<CasparServer>();
    }
}
