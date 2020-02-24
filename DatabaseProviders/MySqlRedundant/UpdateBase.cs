using System;
using System.Data.Common;

namespace TAS.Database.MySqlRedundant
{
    internal abstract class UpdateBase
    {
        private DbTransaction _transaction;
        
        public DbConnectionRedundant Connection { get; set;  }

        public abstract void Update();

        protected int Version
        {
            get
            {
                var type = GetType().Name;
                return int.Parse(type.Substring(6));
            }
        }

        protected void ExecuteScript(string script)
        {
            Connection.ExecuteScript(script);
        }

        protected void BeginTransaction()
        {
            if (_transaction != null)
                throw new ApplicationException("Transaction already started");
            _transaction = Connection.BeginTransaction();
        }

        protected void CommitTransaction()
        {
            using (var cmdUpdateVersion = new DbCommandRedundant($"UPDATE params SET value = \"{Version}\" WHERE SECTION=\"DATABASE\" AND `key`=\"VERSION\"", Connection))
                cmdUpdateVersion.ExecuteNonQuery();
            _transaction.Commit();
            _transaction = null;
        }

        protected void RollbackTransaction()
        {
            if (_transaction == null)
                throw new ApplicationException("No active transaction");
            _transaction?.Rollback();
            _transaction = null;
        }

        protected void SimpleUpdate(string script)
        {
            BeginTransaction();
            try
            {
                ExecuteScript(script);
                CommitTransaction();
            }
            catch 
            {
                RollbackTransaction();
                throw;
            }
        }

    }
}
