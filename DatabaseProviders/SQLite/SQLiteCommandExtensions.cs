using System.Data.SQLite;

namespace TAS.Database.SQLite
{
    public static class SQLiteCommandExtensions
    {
        public static long GetLastInsertedId(this SQLiteCommand command)
        {
            return command.Connection.LastInsertRowId;
        }
    }
}
