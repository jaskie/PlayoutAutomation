//#undef DEBUG
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Linq;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Database.Interfaces;
using TAS.Common.Database.Interfaces.Media;
using TAS.Common.Interfaces;
using TAS.Database.MySqlRedundant.Configurator;

namespace TAS.Database.MySqlRedundant
{
    [Export(typeof(IDatabase))]
    public class DatabaseMySqlRedundant : DatabaseBase
    {
        private Dictionary<string, Dictionary<string, int>> _tablesStringFieldsLenghts;
        private readonly static IUiTemplatesManager _uiTemplatesManager = new DataTemplateManager();
        public DatabaseMySqlRedundant()
        {
            _uiTemplatesManager.LoadDataTemplates();
        }

        public override void Open(ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            ConnectionStringPrimary =  connectionStringSettingsCollection[ConnectionStringsNames.Primary]?.ConnectionString;
            ConnectionStringSecondary = connectionStringSettingsCollection[ConnectionStringsNames.Secondary]?.ConnectionString;
            Close();
            Connection = new DbConnectionRedundant(ConnectionStringPrimary, ConnectionStringSecondary);
            Connection.StateRedundantChange += Connection_StateRedundantChange;
            Connection.Open();
        }

        public void Open(string connectionStringPrimary, string connectionStringSecondary)
        {
            Close();
            Connection = new DbConnectionRedundant(connectionStringPrimary, connectionStringSecondary);
            Connection.StateRedundantChange += Connection_StateRedundantChange;
            Connection.Open();
        }

        public override void InitializeFieldLengths()
        {
            if ((Connection.StateRedundant & (ConnectionStateRedundant.OpenPrimary | ConnectionStateRedundant.OpenSecondary)) != ConnectionStateRedundant.Closed)
                _tablesStringFieldsLenghts = ReadTablesStringFieldLenghts();

            ServerMediaFieldLengths.Add(nameof(IServerMedia.MediaName), _tablesStringFieldsLenghts["servermedia"]["MediaName"]);
            ServerMediaFieldLengths.Add(nameof(IServerMedia.FileName), _tablesStringFieldsLenghts["servermedia"]["FileName"]);
            ServerMediaFieldLengths.Add(nameof(IServerMedia.Folder), _tablesStringFieldsLenghts["servermedia"]["Folder"]);
            ServerMediaFieldLengths.Add(nameof(IServerMedia.IdAux), _tablesStringFieldsLenghts["servermedia"]["idAux"]);

            ArchiveMediaFieldLengths.Add(nameof(IArchiveMedia.MediaName), _tablesStringFieldsLenghts["archivemedia"]["MediaName"]);
            ArchiveMediaFieldLengths.Add(nameof(IArchiveMedia.FileName), _tablesStringFieldsLenghts["archivemedia"]["FileName"]);
            ArchiveMediaFieldLengths.Add(nameof(IArchiveMedia.Folder), _tablesStringFieldsLenghts["archivemedia"]["Folder"]);
            ArchiveMediaFieldLengths.Add(nameof(IArchiveMedia.IdAux), _tablesStringFieldsLenghts["archivemedia"]["idAux"]);

            EventFieldLengths.Add(nameof(IEvent.EventName), _tablesStringFieldsLenghts["rundownevent"]["EventName"]);
            EventFieldLengths.Add(nameof(IEvent.IdAux), _tablesStringFieldsLenghts["rundownevent"]["idAux"]);
            EventFieldLengths.Add(nameof(ICommandScript.Command), _tablesStringFieldsLenghts["rundownevent"]["Commands"]);

            MediaSegmentFieldLengths.Add(nameof(IMediaSegment.SegmentName), _tablesStringFieldsLenghts["mediasegments"]["SegmentName"]);
        }

        private Dictionary<string, Dictionary<string, int>> ReadTablesStringFieldLenghts()
        {
            var tables = Connection.GetSchema("Tables");
            var columns = Connection.GetSchema("Columns");
            var tableNames = tables.Rows.Cast<DataRow>().Select(r => r["TABLE_NAME"].ToString());
            var result = tableNames.ToDictionary(tableName => tableName, tableName => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in columns.Rows)
            {
                var tableName = row["TABLE_NAME"].ToString();
                if (!result.ContainsKey(tableName))
                    continue;
                if (!int.TryParse(row["CHARACTER_MAXIMUM_LENGTH"].ToString(), out var charLength))
                    continue;
                result[tableName].Add(row["COLUMN_NAME"].ToString(), charLength);
            }
            return result;
        }

        protected override string TrimText(string tableName, string columnName, string value)
        {
            if (value == null)
                return null;
            return _tablesStringFieldsLenghts[tableName][columnName] < value.Length
                ? value.Substring(0, _tablesStringFieldsLenghts[tableName][columnName])
                : value;
        }

        public override void Close()
        {
            if (Connection == null)
                return;
            Connection.StateRedundantChange -= Connection_StateRedundantChange;
            Connection.Close();
            Connection = null;
        }

        public string ConnectionStringPrimary { get; private set; }
        public string ConnectionStringSecondary { get; private set; }

        public override ConnectionStateRedundant ConnectionState => Connection.StateRedundant;

        #region Configuration Functions

        private UpdateManager _updateManager;

        internal UpdateManager UpdateManager
        {
            get
            {
                if (_updateManager == null)
                    _updateManager = new UpdateManager(Connection);
                return _updateManager;
            }
        }


        public void TestConnect(string connectionString)
        {
            DbConnectionRedundant.TestConnect(connectionString);
        }

        public bool CreateEmptyDatabase(string connectionString, string collate)
        {
            return DbConnectionRedundant.CreateEmptyDatabase(connectionString, collate);
        }

        public void DropDatabase(string connectionString)
        {
            DbConnectionRedundant.DropDatabase(connectionString);
        }

        public void CloneDatabase(string connectionStringSource, string connectionStringDestination)
        {
            DbConnectionRedundant.CloneDatabase(connectionStringSource, connectionStringDestination);
            DbConnectionRedundant.TestConnect(connectionStringDestination);
        }

        public override bool UpdateRequired()
        {
            return UpdateManager.UpdateRequired();
        }


        public override void UpdateDb()
        {
            UpdateManager.Update();
        }

        #endregion //Configuration functions
        


    }
}
