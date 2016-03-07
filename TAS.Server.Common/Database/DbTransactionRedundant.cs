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
        }

        public override void Rollback()
        {
            if (_transactionPrimary != null)
                _transactionPrimary.Rollback();
            if (_transactionSecondary != null)
                _transactionSecondary.Rollback();
        }

        internal static DbTransactionRedundant Create(DbConnectionRedundant connection)
        {
            return new DbTransactionRedundant() { _connection = connection,
                _transactionPrimary = connection.ConnectionPrimary == null? null: connection.ConnectionPrimary.BeginTransaction(),
                _transactionSecondary = connection.ConnectionSecondary == null ? null : connection.ConnectionSecondary.BeginTransaction()
            };
        }
    }
}
