using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;

namespace TAS.Client.Config.Model
{
    public class PlayoutServers: IDisposable
    {
        readonly IDatabase _db;
        public PlayoutServers(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            _db = DatabaseProviderLoader.LoadDatabaseProviders().FirstOrDefault(db => db.DatabaseType == databaseType);
            _db.Open(connectionStringSettingsCollection);
            Servers = _db.LoadServers<CasparServer>().ToList();
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
                    _db.InsertServer(s);
                else
                    _db.UpdateServer(s);
            });
            DeletedServers.ForEach(s => { if (s.Id > 0) _db.DeleteServer(s); });
        }

        public List<CasparServer> Servers { get; }
        public List<CasparServer> DeletedServers = new List<CasparServer>();
        public void Dispose()
        {
            _db.Close();
        }
    }
}
