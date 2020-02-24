using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Database.MySqlRedundant
{
    internal class UpdateManager
    {
        private readonly DbConnectionRedundant _connection;
        public const int ReqiuredVersion = 13;

        public UpdateManager(DbConnectionRedundant connection)
        {
            _connection = connection;
        }

        public int GetCurrentVersion()
        {
            using (var command = new DbCommandRedundant("select value from params where SECTION=\"DATABASE\" and `key`=\"VERSION\"", _connection))
            {
                var dbVersionNr = 0;
                try
                {
                    string dbVersionStr;
                    lock (_connection)
                        dbVersionStr = (string)command.ExecuteScalar();
                    if (dbVersionStr != null)
                        int.TryParse(dbVersionStr, out dbVersionNr);
                }
                catch
                {
                    // ignored
                }
                return dbVersionNr;
            }
        }

        public bool UpdateRequired()
        {
            return GetCurrentVersion() < ReqiuredVersion;
        }

        public void Update()
        {
            for (int i = GetCurrentVersion() + 1; i <= ReqiuredVersion; i++)
            {
                var updateType = Type.GetType($"TAS.Database.MySqlRedundant.Updates.Update{i:D3}");
                if (updateType == null)
                    throw new ApplicationException($"Missing class to perform update {i}");
                var update = (UpdateBase)Activator.CreateInstance(updateType);
                update.Connection = _connection;
                try
                {
                    update.Update();
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"Update {i} failed", e);
                }
            }
        }

    }
}
