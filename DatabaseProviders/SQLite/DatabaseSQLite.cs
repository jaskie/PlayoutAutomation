﻿using System.Configuration;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using TAS.Common;
using TAS.Database.Common.Interfaces;
using TAS.Database.Common;
using System;

namespace TAS.Database.SQLite
{
    [Export(typeof(IDatabase))]
    public class DatabaseSQLite : DatabaseBase
    {

        private static readonly string DatabaseFile = Path.Combine(FileUtils.ApplicationDataPath, "TVPlay.db");
        private const long ExpectedVersion = 2;
        private long _currentVersion;

        private long GetCurrentVersion()
        {
            using (var cmd = new SQLiteCommand("PRAGMA user_version", Connection))
            {
                return (long)cmd.ExecuteScalar();
            }
        }

        public override ConnectionStateRedundant ConnectionState => (ConnectionStateRedundant)Connection.State;

        public override void Close()
        {
            if (Connection == null)
                return;
            Connection.Close();
            Connection = null;
        }

        public bool CreateEmptyDatabase()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.SQLite.Schema.Schema.sql"))
            using (var reader = new StreamReader(stream))
                ExecuteScript(reader.ReadToEnd());
            return true;
        }

        private void ExecuteScript(string script)
        {
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = script;
                command.ExecuteNonQuery();
            }
        }

        public override void InitializeFieldLengths() { }

        public override void Open(ConnectionStringSettingsCollection connectionStringSettingsCollection)
        {
            var path = FileUtils.ApplicationDataPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = DatabaseFile
            };
            Connection = new SQLiteConnection(builder.ToString());
            Connection.StateChange += Connection_StateChange;
            Connection.Open();
            _currentVersion = GetCurrentVersion();
            if (_currentVersion == 0)
                CreateEmptyDatabase();
        }

        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            Connection_StateRedundantChange(this, new RedundantConnectionStateEventArgs((ConnectionStateRedundant)e.OriginalState, (ConnectionStateRedundant)e.CurrentState));
        }

        public override void UpdateDb()
        {
            var nextVersion = _currentVersion;
            while (nextVersion++ < ExpectedVersion)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"TAS.Database.SQLite.Schema.Update{nextVersion:D4}.sql"))
                using (var reader = new StreamReader(stream))
                    ExecuteScript(reader.ReadToEnd());
                _currentVersion = GetCurrentVersion();
                if (_currentVersion != nextVersion)
                    throw new ApplicationException($"Database update failed. Expected version {nextVersion}, got version {_currentVersion} after the update.");
            }
        }

        public override bool UpdateRequired()
        {
            return _currentVersion < ExpectedVersion;
        }

        protected override string TrimText(string tableName, string columnName, string value)
        {
            return value;
        }
    }
}
