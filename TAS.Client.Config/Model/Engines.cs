using System.Collections.Generic;
using TAS.Database;

namespace TAS.Client.Config.Model
{
    public class Engines
    {
        internal readonly List<CasparServer> Servers;
        internal readonly ArchiveDirectories ArchiveDirectories;
        public readonly string ConnectionStringPrimary;
        public readonly string ConnectionStringSecondary;
        public Engines(string connectionStringPrimary, string connectionStringSecondary)
        {
            ConnectionStringPrimary = connectionStringPrimary;
            ConnectionStringSecondary = connectionStringSecondary;
            ArchiveDirectories = new ArchiveDirectories(connectionStringPrimary, connectionStringSecondary);
            try
            {
                Db.Open();
                EngineList = Db.DbLoadEngines<Engine>();
                Servers = Db.DbLoadServers<CasparServer>();
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
                Db.Close();
            }
        }

        public void Save()
        {
            try
            {
                Db.Open(ConnectionStringPrimary, ConnectionStringSecondary);
                EngineList.ForEach(e =>
                {
                    if (e.IsModified)
                    {
                        if (e.Id == 0)
                            e.DbInsertEngine();
                        else
                            e.DbUpdateEngine();
                    }
                });
                DeletedEngines.ForEach(s => { if (s.Id > 0) s.DbDeleteEngine(); });
            }
            finally
            {
                Db.Close();
            }
        }

        public List<Engine> EngineList { get; }
        public List<Engine> DeletedEngines = new List<Engine>();
    }
}
