using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;

namespace TAS.Client.Config.Model
{
    public class Engines : IDisposable
    {
        internal readonly List<CasparServer> Servers;
        internal readonly ArchiveDirectories ArchiveDirectories;
        private readonly IDatabase _db;

        public Engines(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            _db = DatabaseProviderLoader.LoadDatabaseProviders().FirstOrDefault(db => db.DatabaseType == databaseType);
            _db.Open(connectionStringSettingsCollection);
            ArchiveDirectories = new ArchiveDirectories(_db);
            EngineList = _db.LoadEngines<Engine>();
            Servers = _db.LoadServers<CasparServer>();
            Servers.ForEach(s =>
            {
                s.Channels.ForEach(c => c.Owner = s);
                s.Recorders.ForEach(r => r.Owner = s);
            });
            EngineList.ForEach(e =>
            {
                e.IsNew = false;
                e.Servers = Servers;
                e.ArchiveDirectories = ArchiveDirectories;
            });
        }

        public void Save()
        {
            EngineList.ForEach(e =>
            {
                if (e.IsModified)
                {
                    if (e.Id == 0)
                        _db.InsertEngine(e);
                    else
                        _db.UpdateEngine(e);
                }
            });
            DeletedEngines.ForEach(s =>
            {
                if (s.Id > 0) _db.DeleteEngine(s);
            });
        }

        public List<Engine> EngineList { get; }
        public List<Engine> DeletedEngines = new List<Engine>();

        public void Dispose()
        {
            _db.Close();
        }
    }
}
