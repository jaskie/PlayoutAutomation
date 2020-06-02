using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TAS.Common;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class Engines : IDisposable
    {
        internal readonly IReadOnlyCollection<CasparServer> Servers;
        internal readonly ArchiveDirectories ArchiveDirectories;
        private readonly IDatabase _db;

        public Engines(DatabaseType databaseType, ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            _db = DatabaseLoader.LoadDatabaseProviders().FirstOrDefault(db => db.DatabaseType == databaseType);
            _db.Open(connectionStringSettingsCollection);
            ArchiveDirectories = new ArchiveDirectories(_db);
            EngineList = _db.LoadEngines<Engine>().ToList();
            Servers = _db.LoadServers<CasparServer>();
            foreach (var s in Servers)
            {
                s.Channels.ForEach(c => c.Owner = s);
                s.Recorders.ForEach(r => r.Owner = s);
            }
            foreach (var e in EngineList)
            {
                e.IsNew = false;
                e.Servers = Servers;
                e.ArchiveDirectories = ArchiveDirectories;
            }
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
