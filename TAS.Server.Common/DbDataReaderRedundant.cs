using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace TAS.Server.Common
{
    public class DbDataReaderRedundant : DbDataReader
    {
        private readonly MySqlDataReader _activeReader;
        private readonly DbCommandRedundant _command;

        internal DbDataReaderRedundant(DbCommandRedundant command, CommandBehavior behavior)
        {
            _command = command;
            try
            {
                _activeReader = command.CommandPrimary.ExecuteReader(behavior);
            }
            catch
            {
                _activeReader = command.CommandSecondary.ExecuteReader(behavior);
            }
        }

        public override object this[string name] { get { return _activeReader[name]; } }

        public override object this[int ordinal] { get { return _activeReader[ordinal]; } }
        public override int Depth { get { return _activeReader.Depth; } }
        public override int FieldCount { get { return _activeReader.FieldCount; } }
        public override bool HasRows { get { return _activeReader.HasRows; } }
        public override bool IsClosed { get { return _activeReader.IsClosed; } }
        public override int RecordsAffected { get { return _activeReader.RecordsAffected; } }
        public override void Close() { _activeReader.Close(); }

        public override bool GetBoolean(int ordinal) { return _activeReader.GetBoolean(ordinal); } 
        public override byte GetByte(int ordinal) { return _activeReader.GetByte(ordinal); }
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _activeReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal) { return _activeReader.GetChar(ordinal); }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _activeReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _activeReader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return _activeReader.GetDateTime(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return _activeReader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return _activeReader.GetDouble(ordinal);
        }

        public override IEnumerator GetEnumerator()
        {
            return _activeReader.GetEnumerator();
        }

        public override Type GetFieldType(int ordinal)
        {
            return _activeReader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return _activeReader.GetFloat(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return _activeReader.GetGuid(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return _activeReader.GetInt16(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return _activeReader.GetInt32(ordinal);
        }

        public int GetInt32(string name)
        {
            return _activeReader.GetInt32(name);
        }

        public override long GetInt64(int ordinal)
        {
            return _activeReader.GetInt64(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _activeReader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _activeReader.GetOrdinal(name);
        }

        public override DataTable GetSchemaTable()
        {
            return _activeReader.GetSchemaTable();
        }

        public ulong GetUInt64(string name)
        {
            return _activeReader.GetUInt64(name);
        }

        public override string GetString(int ordinal)
        {
            return _activeReader.GetString(ordinal);
        }

        public string GetString(string name)
        {
            return _activeReader.GetString(name);
        }

        public override object GetValue(int ordinal)
        {
            return _activeReader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return _activeReader.GetValues(values);
        }

        public override bool IsDBNull(int ordinal)
        {
            return _activeReader.IsDBNull(ordinal);
        }

        public override bool NextResult()
        {
            return _activeReader.NextResult();
        }

        public override bool Read()
        {
            return _activeReader.Read();
        }
    }
}
