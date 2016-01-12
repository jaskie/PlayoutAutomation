using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Database;

namespace TAS.Client.Setup.Model
{
    public class Engines
    {
        readonly List<Engine> _engines;
        internal readonly List<CasparServer> Servers;
        private readonly string _connectionStringPrimary;
        private readonly string _connectionStringSecondary;
        public Engines(string connectionStringPrimary, string connectionStringSecondary)
        {
            _connectionStringPrimary = connectionStringPrimary;
            _connectionStringSecondary = connectionStringSecondary;
            try
            {
                Database.Open(connectionStringPrimary, connectionStringSecondary);
                _engines = Database.DbLoadEngines<Engine>();
                Servers = Database.DbLoadServers<CasparServer>();
                Servers.ForEach(s => s.Channels.ForEach(c => c.Owner = s));
                _engines.ForEach(e =>
                    {
                        e.IsNew = false;
                        e.Servers = this.Servers;
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
                Database.Open(_connectionStringPrimary, _connectionStringSecondary);
                _engines.ForEach(e =>
                {
                    if (e.Modified)
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
