using System.Collections.Generic;

namespace TAS.Client.Config.Model
{
    public class PlayoutServers
    {
        readonly Database.Db _db;
        public PlayoutServers(string connectionStringPrimary, string connectionStringSecondary)
        {
            _db = new Database.Db();
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
