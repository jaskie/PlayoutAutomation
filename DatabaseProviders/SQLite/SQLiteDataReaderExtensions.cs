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
            return (Int32)GetValue<Int64>(dataReader, name);
        }
        public static UInt32 GetUInt32(this SQLiteDataReader dataReader, string name)
        {
            return (UInt32)GetValue<Int64>(dataReader, name);
        }
        public static bool IsDBNull(this SQLiteDataReader dataReader, string name)
        {
            return dataReader.IsDBNull(dataReader.GetOrdinal(name));
        }
        public static DateTime GetDateTime(this SQLiteDataReader dataReader, string name)
        {
            return new DateTime(GetValue<Int64>(dataReader, name));
        }
        public static Guid GetGuid(this SQLiteDataReader dataReader, string name)
        {
            var bytes = GetValue<Byte[]>(dataReader, name);
            return bytes == null ? Guid.Empty : new Guid(bytes);
        }
        public static long GetInt64(this SQLiteDataReader dataReader, string name)
        {
            return GetValue<Int64>(dataReader, name);
        }
        public static UInt64 GetUInt64(this SQLiteDataReader dataReader, string name)
        {
            return (UInt64)GetValue<Int64>(dataReader, name);
        }
        public static string GetString(this SQLiteDataReader dataReader, string name)
        {
            return GetValue<string>(dataReader, name);
        }
        public static byte GetByte(this SQLiteDataReader dataReader, string name)
        {
            return (byte)GetValue<Int64>(dataReader, name);
        }
        public static sbyte GetSByte(this SQLiteDataReader dataReader, string name)
        {
            return (sbyte)GetValue<Int64>(dataReader, name);
        }
        public static double GetDouble(this SQLiteDataReader dataReader, string name)
        {
            return (double)GetValue<decimal>(dataReader, name);
        }
        public static Int16 GetInt16(this SQLiteDataReader dataReader, string name)
        {
            return (Int16)GetValue<Int64>(dataReader, name);
        }
        public static UInt16 GetUInt16(this SQLiteDataReader dataReader, string name)
        {
            return (UInt16)GetValue<Int64>(dataReader, name);
        }
        public static TimeSpan GetTimeSpan(this SQLiteDataReader dataReader, string name)
        {
            return new TimeSpan(GetValue<Int64>(dataReader, name));
        }

        private static T GetValue<T>(SQLiteDataReader dataReader, string name)
        {
            var index = dataReader.GetOrdinal(name);
            return dataReader.IsDBNull(index) ? default : dataReader.GetFieldValue<T>(index);
        }
    }
}
