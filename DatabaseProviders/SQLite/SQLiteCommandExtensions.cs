using System.Data.SQLite;

namespace TAS.Database.SQLite
{
    public static class SQLiteCommandExtensions
    {
        public static long LastInsertedId(this SQLiteCommand command)
        {
            return command.Connection.LastInsertRowId;
        }
    }
}
