using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using TAS.Common;

namespace TAS.Database.MySqlRedundant
{

    [DesignerCategory("Code")]
    internal class DbCommandRedundant : DbCommand
    {
        private enum StatementType
        {
            Select,
            Insert,
            Update,
            Delete,
            Other
        }

        private DbParameterCollectionRedundant _parameters;
        private StatementType _statementType;
        private CommandType _commandType;
        private string _commandText;
        private bool _designTimeVisible;
        private int _commandTimeout;
        private UpdateRowSource _updatedRowSource;
        private DbConnectionRedundant _dbConnection;


        #region Constructors

        public DbCommandRedundant()
        {
            CommandPrimary = new MySqlCommand();
            CommandSecondary = new MySqlCommand();
        }

        public DbCommandRedundant(string commandText)
        {
            _commandText = commandText.Trim();
            _statementType = _determineStatementType(commandText);
            CommandPrimary = new MySqlCommand(commandText);
            CommandSecondary = new MySqlCommand(commandText);
        }

        public DbCommandRedundant(string commandText, DbConnectionRedundant connection)
        {
            _dbConnection = connection;
            _commandText = commandText.Trim();
            _statementType = _determineStatementType(commandText);
            CommandPrimary = new MySqlCommand(commandText, connection.ConnectionPrimary);
            CommandSecondary = new MySqlCommand(commandText, connection.ConnectionSecondary);
        }

        internal DbCommandRedundant(DbConnectionRedundant connection)
        {
            _dbConnection = connection;
            CommandPrimary = new MySqlCommand { Connection = connection.ConnectionPrimary };
            CommandSecondary = new MySqlCommand { Connection = connection.ConnectionSecondary };
        }

        #endregion //Constructors

        #region DbCommand implementation

        public override string CommandText
        {
            get => _commandText;
            set
            {
                if (_commandText != value)
                {
                    _commandText = value.Trim();
                    _statementType = _determineStatementType(value);
                    if (CommandPrimary != null)
                        CommandPrimary.CommandText = value;
                    if (CommandSecondary != null)
                        CommandSecondary.CommandText = value;
                }
            }
        }

        public override int CommandTimeout
        {
            get => _commandTimeout;
            set
            {
                if (_commandTimeout == value) return;
                _commandTimeout = value;
                CommandPrimary.CommandTimeout = value;
                CommandSecondary.CommandTimeout = value;
            }
        }

        public override CommandType CommandType
        {
            get => _commandType;
            set
            {
                if (_commandType == value) return;
                _commandType = value;
                CommandPrimary.CommandType = value;
                CommandSecondary.CommandType = value;
            }
        }

        public override bool DesignTimeVisible
        {
            get => _designTimeVisible;
            set
            {
                if (_designTimeVisible == value) return;
                _designTimeVisible = value;
                CommandPrimary.DesignTimeVisible = value;
                CommandSecondary.DesignTimeVisible = value;
            }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _updatedRowSource;
            set
            {
                if (_updatedRowSource == value) return;
                _updatedRowSource = value;
                CommandPrimary.UpdatedRowSource = value;
                CommandSecondary.UpdatedRowSource = value;
            }
        }

        protected override DbConnection DbConnection
        {
            get => _dbConnection;
            set
            {
                if (_dbConnection == value || !(value is DbConnectionRedundant connectionRedundant)) return;
                _dbConnection = connectionRedundant;
                CommandPrimary.Connection = connectionRedundant.ConnectionPrimary;
                CommandSecondary.Connection = connectionRedundant.ConnectionSecondary;
            }
        }

        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();

        public new DbParameterCollectionRedundant Parameters => _parameters ?? (_parameters = new DbParameterCollectionRedundant());

        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteNonQuery()
        {
            FillParameters();
            var retPrimary = 0;
            if (IsConnectionValid(CommandPrimary.Connection))
                retPrimary = CommandPrimary.ExecuteNonQuery();
            if (_statementType == StatementType.Select || !IsConnectionValid(CommandSecondary.Connection))
                return retPrimary;
            var retSecondary = CommandSecondary.ExecuteNonQuery();
            if (retSecondary != retPrimary)
                _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
            return retPrimary;
        }

        public override object ExecuteScalar()
        {
            FillParameters();
            object retPrimary = null;
            if (IsConnectionValid(CommandPrimary.Connection))
                retPrimary = CommandPrimary.ExecuteScalar();
            if (_statementType == StatementType.Select || !IsConnectionValid(CommandSecondary.Connection))
                return retPrimary;
            var retSecondary = CommandSecondary.ExecuteScalar();
            if (retSecondary != retPrimary)
                _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
            CommandSecondary.ExecuteScalar();
            return retPrimary;
        }

        public override void Prepare()
        {
            FillParameters();
            if (IsConnectionValid(CommandPrimary.Connection))
                CommandPrimary.Prepare();
            if (_statementType != StatementType.Select && IsConnectionValid(CommandSecondary.Connection))
                CommandSecondary.Prepare();
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            FillParameters();
            if (IsConnectionValid(CommandPrimary.Connection))
                return new DbDataReaderRedundant(CommandPrimary, behavior);
            if (IsConnectionValid(CommandSecondary.Connection))
                return new DbDataReaderRedundant(CommandSecondary, behavior);
            throw new DataException("No valid connection to execute reader");
        }

        public new DbDataReaderRedundant ExecuteReader()
        {
            return (DbDataReaderRedundant)base.ExecuteReader();
        }

        public new DbDataReaderRedundant ExecuteReader(CommandBehavior behavior)
        {
            return (DbDataReaderRedundant)base.ExecuteReader(behavior);
        }

        #endregion

        public long LastInsertedId()
        {
                var primaryId = CommandPrimary.LastInsertedId;
                if (IsConnectionValid(CommandSecondary.Connection) && CommandSecondary.LastInsertedId != primaryId)
                    _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
                return primaryId;
        }

        internal MySqlCommand CommandPrimary { get; }

        internal MySqlCommand CommandSecondary { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CommandPrimary.Dispose();
                CommandSecondary.Dispose();
            }
            base.Dispose(disposing);
        }

        #region utilities

        private StatementType _determineStatementType(string commandText)
        {
            var statement = commandText.Substring(0, commandText.IndexOf(' ')).Trim().ToLowerInvariant();
            switch (statement)
            {
                case "select":
                    return StatementType.Select;
                case "insert":
                    return StatementType.Insert;
                case "update":
                    return StatementType.Update;
                case "delete":
                    return StatementType.Delete;
            }
            return StatementType.Other;
        }

        private bool IsConnectionValid(MySqlConnection connection)
        {
            return connection != null
                   && connection.State == ConnectionState.Open;
        }

        private void FillParameters()
        {
            if (_parameters != null)
                foreach (var parameter in _parameters)
                {
                    object convertedValue = parameter.Value;
                    if (parameter.Value == null)
                        convertedValue = DBNull.Value;
                    if (parameter.Value is Guid)
                        convertedValue = ((Guid)parameter.Value).ToByteArray();
                    CommandPrimary.Parameters.AddWithValue(parameter.Key, convertedValue);
                    CommandSecondary.Parameters.AddWithValue(parameter.Key, convertedValue);
                }
        }

        #endregion // utilities

    }
}
