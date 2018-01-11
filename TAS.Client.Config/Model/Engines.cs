using System.Collections.Generic;

namespace TAS.Client.Config.Model
{
    public class Engines
    {
        internal readonly List<CasparServer> Servers;
        internal readonly ArchiveDirectories ArchiveDirectories;
        public readonly string ConnectionStringPrimary;
        public readonly string ConnectionStringSecondary;
        private readonly Database.Db _db;
        public Engines(string connectionStringPrimary, string connectionStringSecondary)
        {
            ConnectionStringPrimary = connectionStringPrimary;
            ConnectionStringSecondary = connectionStringSecondary;
            _db = new Database.Db();
            _db.Open(connectionStringPrimary, connectionStringSecondary);
            ArchiveDirectories = new ArchiveDirectories(_db);
            try
            {
                _db.Open();
                EngineList = _db.DbLoadEngines<Engine>();
                Servers = _db.DbLoadServers<CasparServer>();
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
            finally
            {
                _db.Close();
            }
        }

        public void Save()
        {
            try
            {
                _db.Open(ConnectionStringPrimary, ConnectionStringSecondary);
                EngineList.ForEach(e =>
                {
                    if (e.IsModified)
                    {
                        if (e.Id == 0)
                            _db.DbInsertEngine(e);
                        else
                            _db.DbUpdateEngine(e);
                    }
                });
                DeletedEngines.ForEach(s => { if (s.Id > 0) _db.DbDeleteEngine(s); });
            }
            finally
            {
                _db.Close();
            }
        }

        public List<Engine> EngineList { get; }
        public List<Engine> DeletedEngines = new List<Engine>();
    }
}
