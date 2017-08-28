using System.Data.Common;
using System.IO;

namespace TAS.Database
{
    static class DbSchema
    {

        public static Stream GetSchemaDefinitionStream()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TAS.Database.database.sql");
        }

        public static bool Update(this DbConnection connection)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT `value` FROM params WHERE `section`=\"DATABASE\" AND `key`=\"VERSION\";";
            object version = command.ExecuteScalar();
            return true;
        }
    }
}
