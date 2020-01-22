using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Database.SQLite
{
    public static class SQLiteDataReaderExtensions
    {
        public static Int32 GetInt32(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.GetInt32(dataReader.GetOrdinal(name));
        }
        public static uint GetUInt32(this SQLiteDataReader dataReader, string name)
        {
            return (uint)dataReader.GetInt32(dataReader.GetOrdinal(name));
        }
        public static bool IsDBNull(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.IsDBNull(dataReader.GetOrdinal(name));
        }
        public static DateTime GetDateTime(this SQLiteDataReader dataReader, string name)
        {
            var index = dataReader.GetOrdinal(name);
            if (dataReader.IsDBNull(index))
                return new DateTime();
            else
                return new DateTime(dataReader.GetDateTime(index).Ticks);
        }
        public static Guid GetGuid(this SQLiteDataReader dataReader, string name)
        {
            var index = dataReader.GetOrdinal(name);
            return dataReader.IsDBNull(index) ? new Guid() : dataReader.GetGuid(index);
        }
        public static long GetInt64(this SQLiteDataReader dataReader, string name)
        {
            return (long)dataReader.GetInt64(dataReader.GetOrdinal(name));
        }
        public static UInt64 GetUInt64(this SQLiteDataReader dataReader, string name)
        {
            var index = dataReader.GetOrdinal(name);
            return dataReader.IsDBNull(index) ? 0UL : (ulong)dataReader.GetInt64(index);
        }
        public static string GetString(this SQLiteDataReader dataReader, string name)
        {
            var index = dataReader.GetOrdinal(name);
            return dataReader.IsDBNull(index) ? "" : dataReader.GetString(index);
        }
        public static byte GetByte(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.GetByte(dataReader.GetOrdinal(name));
        }
        public static double GetDouble(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.GetDouble(dataReader.GetOrdinal(name));
        }
        public static short GetInt16(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.GetInt16(dataReader.GetOrdinal(name));
        }
        public static TimeSpan GetTimeSpan(this SQLiteDataReader dataReader, string name)
        { 
            var index = dataReader.GetOrdinal(name);
            return dataReader.IsDBNull(index) ? new TimeSpan() : new TimeSpan((long)dataReader.GetInt64(index));
        }
    }
}
