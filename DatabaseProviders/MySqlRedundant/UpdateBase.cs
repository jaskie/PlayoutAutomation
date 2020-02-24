using System;
using System.Data.Common;

namespace TAS.Database.MySqlRedundant
{
    internal abstract class UpdateBase
    {
        private DbTransaction _transaction;

        public abstract void Update(DbConnectionRedundant connection);

        protected int Version
        {
            get
            {
                var type = GetType().Name;
                return int.Parse(type.Substring(6));
            }
        }

        protected void ExecuteScript(string script, DbConnectionRedundant connection)
        {
            connection.ExecuteScript(script);
        }

        protected void BeginTransaction(DbConnection connection)
        {
            if (_transaction != null)
                throw new ApplicationException("Transaction already started");
            _transaction = connection.BeginTransaction();
        }

        protected void Commit(DbConnectionRedundant connection)
        {
            using (var cmdUpdateVersion = new DbCommandRedundant($"UPDATE params SET value = \"{Version}\" WHERE SECTION=\"DATABASE\" AND `key`=\"VERSION\"", connection))
                cmdUpdateVersion.ExecuteNonQuery();
            _transaction.Commit();
            _transaction = null;
        }

        protected void Rollback()
        {
            if (_transaction == null)
                throw new ApplicationException("No active transaction");
            _transaction?.Rollback();
            _transaction = null;
        }

        protected void SimpleUpdate(DbConnectionRedundant connection, string script)
        {
            BeginTransaction(connection);
            try
            {
                ExecuteScript(script, connection);
                Commit(connection);
            }
            catch 
            {
                Rollback();
                throw;
            }
        }

    }
}
