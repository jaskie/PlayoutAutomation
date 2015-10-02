using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;

namespace TAS.Client.Setup.Model
{
    public class Engines
    {
        readonly string _connectionString;
        readonly List<Engine> _engines;
        internal readonly List<CasparServer> Servers;
        public Engines(string connectionString)
        {
            this._connectionString = connectionString;
            try
            {
                Database.Initialize(connectionString);
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
                Database.Uninitialize();
            }
        }

        public void Save()
        {
            try
            {
                Database.Initialize(_connectionString);
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
                Database.Uninitialize();
            }
        }

        public List<Engine> EngineList { get { return _engines; } }
        public List<Engine> DeletedEngines = new List<Engine>();
    }
}
