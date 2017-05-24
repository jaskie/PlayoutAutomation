using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace TAS.Server.Common.Database
{


    [DesignerCategory("Code")]
    public class DbCommandRedundant : DbCommand
    {
        private readonly MySqlCommand _commandPrimary;
        private readonly MySqlCommand _commandSecondary;
        
        #region Constructors

        public DbCommandRedundant()
        {
            _commandPrimary = new MySqlCommand();
            _commandSecondary = new MySqlCommand();
        }

        public DbCommandRedundant(string commandText)
        {
            CommandText = commandText;
            _commandPrimary = new MySqlCommand(commandText);
            _commandSecondary = new MySqlCommand(commandText);
        }

        public DbCommandRedundant(string commandText, DbConnectionRedundant connection)
        {
            _dbConnection = connection;
            CommandText = commandText;
            _commandPrimary = new MySqlCommand(commandText, connection.ConnectionPrimary);
            _commandSecondary = new MySqlCommand(commandText, connection.ConnectionSecondary);
        }

        internal DbCommandRedundant(DbConnectionRedundant connection)
        {
            _dbConnection = connection;
            _commandPrimary = new MySqlCommand() { Connection = connection.ConnectionPrimary };
            _commandSecondary = new MySqlCommand() { Connection = connection.ConnectionSecondary };
        }

        #endregion //Constructors

        #region utilities

        private enum StatementType
        {
            Select,
            Insert,
            Update,
            Delete,
            Other
        }

        private StatementType _determineStatementType(ref string commandText)
        {
            string statement = commandText.Substring(0, commandText.IndexOf(' ')).Trim().ToLowerInvariant();
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

        private bool _isConnectionValid(MySqlConnection connection)
        {
            return connection != null
                && connection.State == ConnectionState.Open;
        }

        private void _fillParameters()
        {
            if (_parameters != null)
                foreach (var parameter in _parameters)
                {
                    object convertedValue = parameter.Value;
                    if (parameter.Value == null)
                        convertedValue = DBNull.Value;
                    if (parameter.Value is Guid)
                        convertedValue = ((Guid)parameter.Value).ToByteArray();
                    _commandPrimary.Parameters.AddWithValue(parameter.Key, convertedValue);
                    _commandSecondary.Parameters.AddWithValue(parameter.Key, convertedValue);
                }
        }

        #endregion // utilities

        public new void Dispose()
        {
            _commandPrimary.Dispose();
            _commandSecondary.Dispose();
        }


        private StatementType _statementType;

        #region DbCcmmand implementation

        private string _commandText;
        public override string CommandText
        {
            get { return _commandText; }
            set
            {
                if (_commandText != value)
                {
                    _commandText = value.Trim();
                    _statementType = _determineStatementType(ref value);
                    if (_commandPrimary != null)
                        _commandPrimary.CommandText = value;
                    if (_commandSecondary != null)
                        _commandSecondary.CommandText = value;
                }
            }
        }

        private int _commandTimeout;
        public override int CommandTimeout
        {
            get { return _commandTimeout; }
            set
            {
                if (_commandTimeout != value)
                {
                    _commandTimeout = value;
                    _commandPrimary.CommandTimeout = value;
                    _commandSecondary.CommandTimeout = value;
                }
            }
        }

        private CommandType _commandType;
        public override CommandType CommandType
        {
            get { return _commandType; }
            set
            {
                if (_commandType != value)
                {
                    _commandType = value;
                    _commandPrimary.CommandType = value;
                    _commandSecondary.CommandType = value;
                }
            }
        }

        private bool _designTimeVisible;
        public override bool DesignTimeVisible
        {
            get { return _designTimeVisible; }
            set
            {
                if (_designTimeVisible != value)
                {
                    _designTimeVisible = value;
                    _commandPrimary.DesignTimeVisible = value;
                    _commandSecondary.DesignTimeVisible = value;
                }
            }
        }

        private UpdateRowSource _updatedRowSource;
        public override UpdateRowSource UpdatedRowSource
        {
            get { return _updatedRowSource; }
            set
            {
                if (_updatedRowSource != value)
                {
                    _updatedRowSource = value;
                    _commandPrimary.UpdatedRowSource = value;
                    _commandSecondary.UpdatedRowSource = value;
                }
            }
        }

        private DbConnectionRedundant _dbConnection;
        protected override DbConnection DbConnection
        {
            get { return _dbConnection; }
            set
            {
                var connectionRedundant = value as DbConnectionRedundant;
                if (_dbConnection != value && connectionRedundant != null)
                {
                    _dbConnection = connectionRedundant;
                    _commandPrimary.Connection = connectionRedundant.ConnectionPrimary;
                    _commandSecondary.Connection = connectionRedundant.ConnectionSecondary;
                }
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private DbParameterCollectionRedundant _parameters;
        public new DbParameterCollectionRedundant Parameters {
            get
            {
                if (_parameters == null)
                    _parameters = new DbParameterCollectionRedundant();
                return _parameters;
            }
        }

        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        public override int ExecuteNonQuery()
        {
            _fillParameters();
            int retPrimary = 0;
            if (_isConnectionValid(_commandPrimary.Connection))
                retPrimary = _commandPrimary.ExecuteNonQuery();
            if (_statementType != StatementType.Select && _isConnectionValid(_commandSecondary.Connection))
            {
                int retSecondary = 0;
                retSecondary = _commandSecondary.ExecuteNonQuery();
                if (retSecondary != retPrimary)
                    _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
            }
            return retPrimary;
        }

        public override object ExecuteScalar()
        {
            _fillParameters();
            object retPrimary = null;
            if (_isConnectionValid(_commandPrimary.Connection))
                retPrimary = _commandPrimary.ExecuteScalar();
            if (_statementType != StatementType.Select && _isConnectionValid(_commandSecondary.Connection))
            {
                object retSecondary = 0;
                retSecondary = _commandSecondary.ExecuteScalar();
                if (retSecondary != retPrimary)
                    _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
                _commandSecondary.ExecuteScalar();
            }
            return retPrimary;
        }

        public override void Prepare()
        {
            _fillParameters();
            if (_isConnectionValid(_commandPrimary.Connection))
                _commandPrimary.Prepare();
            if (_statementType != StatementType.Select && _isConnectionValid(_commandSecondary.Connection))
                _commandSecondary.Prepare();
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _fillParameters();
            if (_isConnectionValid(_commandPrimary.Connection))
                return new DbDataReaderRedundant(_commandPrimary, behavior);
            else
            if (_isConnectionValid(_commandSecondary.Connection))
                return new DbDataReaderRedundant(_commandSecondary, behavior);
            else
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

        internal MySqlCommand CommandPrimary { get { return _commandPrimary; } }
        internal MySqlCommand CommandSecondary { get { return _commandSecondary; } }

        public long LastInsertedId
        {
            get
            {
                var primaryId = _commandPrimary.LastInsertedId;
                if (_isConnectionValid(_commandSecondary.Connection) && _commandSecondary.LastInsertedId != primaryId)
                    _dbConnection.StateRedundant |= ConnectionStateRedundant.Desynchronized;
                return primaryId;
            }
        }
    }
}
