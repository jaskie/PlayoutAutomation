using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace TAS.Server.Database
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
                if (_transactionPrimary != null)
                    _transactionPrimary.Dispose();
                if (_transactionSecondary != null)
                    _transactionSecondary.Dispose();
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
            if (_transactionPrimary != null)
                _transactionPrimary.Commit();
            if (_transactionSecondary != null)
                _transactionSecondary.Commit();
            _connection.IsActiveTransaction = false;
        }

        public override void Rollback()
        {
            if (_transactionPrimary != null)
                _transactionPrimary.Rollback();
            if (_transactionSecondary != null)
                _transactionSecondary.Rollback();
            _connection.IsActiveTransaction = false;
        }


        internal static DbTransactionRedundant Create(DbConnectionRedundant connection)
        {
            if (connection.IsActiveTransaction)
                throw new InvalidOperationException("Nested transactions are not supported");
            connection.IsActiveTransaction = true;
            return new DbTransactionRedundant()
            {
                _connection = connection,
                _transactionPrimary = connection.ConnectionPrimary?.State == ConnectionState.Open ? connection.ConnectionPrimary.BeginTransaction() : null,
                _transactionSecondary = connection.ConnectionSecondary?.State == ConnectionState.Open ? connection.ConnectionSecondary.BeginTransaction() : null
            };
        }
    }
}
