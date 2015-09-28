using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace TAS.Server.Common
{
    public class Database
    {
        public static bool TestConnect(string connectionString)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                        return true;
                }
                catch { }
            }
            return false;
        }

        public static bool CreateEmptyDatabase(string connectionString)
        {
            MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(connectionString);
            string databaseName = csb.Database;
            string charset = csb.CharacterSet;
            if (string.IsNullOrWhiteSpace(databaseName))
                return false;
            csb.Remove("Database");
            csb.Remove("CharacterSet");
            using (MySqlConnection connection = new MySqlConnection(csb.ConnectionString))
            {
                    connection.Open();
                    var cmd = new MySqlCommand(string.Format("CREATE DATABASE {0} CHARACTER SET = {1};", databaseName, charset), connection);
                    return cmd.ExecuteNonQuery() == 1;
            }
        }
    }
}
