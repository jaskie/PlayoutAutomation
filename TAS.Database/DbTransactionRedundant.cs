using System;
using System.Data;
using System.Data.Common;

namespace TAS.Database
{
    class DbTransactionRedundant : DbTransaction
    {
        MySql.Data.MySqlClient.MySqlTransaction _transactionPrimary;
        MySql.Data.MySqlClient.MySqlTransaction _transactionSecondary;
        DbConnectionRedundant _connection;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    _connection.ActiveTransaction = null;
                    if (_transactionPrimary != null)
                        _transactionPrimary.Dispose();
                    if (_transactionSecondary != null)
                        _transactionSecondary.Dispose();
                }
                catch { }
            }
        }

        public override IsolationLevel IsolationLevel
        {
            get
            {
                return _transactionPrimary.IsolationLevel;
            }
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return _connection;
            }
        }

        public override void Commit()
        {
            _connection.ActiveTransaction = null;
            try
            {
                if (_transactionPrimary != null)
                    _transactionPrimary.Commit();
                if (_transactionSecondary != null)
                    _transactionSecondary.Commit();
            }
            catch { }
        }

        public override void Rollback()
        {
            _connection.ActiveTransaction = null;
            try
            {
                if (_transactionPrimary != null)
                    _transactionPrimary.Rollback();
                if (_transactionSecondary != null)
                    _transactionSecondary.Rollback();
            }
            catch { }
        }

        internal DbTransactionRedundant(DbConnectionRedundant connection)
        {
            if (connection.ActiveTransaction != null)
                throw new InvalidOperationException("Nested transactions are not supported");
            connection.ActiveTransaction = this;
            try
            {
                _transactionPrimary = connection.ConnectionPrimary?.State == ConnectionState.Open ? connection.ConnectionPrimary.BeginTransaction() : null;
                _transactionSecondary = connection.ConnectionSecondary?.State == ConnectionState.Open ? connection.ConnectionSecondary.BeginTransaction() : null;
            }
            catch { }
            _connection = connection;
        }
    }
}
