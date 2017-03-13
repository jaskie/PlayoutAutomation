using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Database;

namespace TAS.Client.Config.Model
{
    public class Engines
    {
        readonly List<Engine> _engines;
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
                Database.Open();
                _engines = Database.DbLoadEngines<Engine>();
                Servers = Database.DbLoadServers<CasparServer>();
                Servers.ForEach(s =>
                {
                    s.Channels.ForEach(c => c.Owner = s);
                    s.Recorders.ForEach(r => r.Owner = s);
                });
                _engines.ForEach(e =>
                    {
                        e.IsNew = false;
                        e.Servers = this.Servers;
                        e.ArchiveDirectories = this.ArchiveDirectories;
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
                Database.Open(ConnectionStringPrimary, ConnectionStringSecondary);
                _engines.ForEach(e =>
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
                Database.Close();
            }
        }

        public List<Engine> EngineList { get { return _engines; } }
        public List<Engine> DeletedEngines = new List<Engine>();
    }
}
