using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace TAS.Database
{
    public class DbDataReaderRedundant : DbDataReader
    {
        private readonly MySqlDataReader _reader;
        private readonly MySqlCommand _command;

        internal DbDataReaderRedundant(MySqlCommand command, CommandBehavior behavior)
        {
            _command = command;
            _reader = command.ExecuteReader(behavior);
        }

        public override object this[string name] => _reader[name];

        public override object this[int ordinal] => _reader[ordinal];
        public override int Depth => _reader.Depth;
        public override int FieldCount => _reader.FieldCount;
        public override bool HasRows => _reader.HasRows;
        public override bool IsClosed => _reader.IsClosed;
        public override int RecordsAffected => _reader.RecordsAffected;
        public override void Close() { _reader.Close(); }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _reader.Dispose();
                _command.Dispose();
            }
        }

        #region field values
        public override bool GetBoolean(int ordinal)
        {
            return _reader.GetBoolean(ordinal);
        }
        public override byte GetByte(int ordinal)
        {
            return _reader.GetByte(ordinal);
        }

        public byte GetByte(string name)
        {
            return _reader.GetByte(name);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal) { return _reader.GetChar(ordinal); }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _reader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return GetDateTime(ordinal, DateTimeKind.Utc);
        }

        public DateTime GetDateTime(int ordinal, DateTimeKind kind )
        {
            return _reader.IsDBNull(ordinal) ? default(DateTime) : DateTime.SpecifyKind(_reader.GetDateTime(ordinal), kind);
        }

        public DateTime GetDateTime(string name, DateTimeKind kind = DateTimeKind.Utc)
        {
            int columnIndex = _reader.GetOrdinal(name);
            return _reader.IsDBNull(columnIndex) ? default(DateTime) : DateTime.SpecifyKind(_reader.GetDateTime(columnIndex), kind);
        }

        public TimeSpan GetTimeSpan(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? TimeSpan.Zero : _reader.GetTimeSpan(index);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return _reader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return _reader.GetDouble(ordinal);
        }

        public override IEnumerator GetEnumerator()
        {
            return _reader.GetEnumerator();
        }

        public override Type GetFieldType(int ordinal)
        {
            return _reader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return _reader.GetFloat(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return _reader.GetGuid(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return _reader.GetInt16(ordinal);
        }

        public short GetInt16(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? (short)0 : _reader.GetInt16(index);
        }

        public ushort GetUInt16(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? (ushort)0 : _reader.GetUInt16(index);
        }

        public override int GetInt32(int ordinal)
        {
            return _reader.GetInt32(ordinal);
        }

        public int GetInt32(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? 0 : _reader.GetInt32(index);
        }

        public uint GetUInt32(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index)? uint.MinValue: _reader.GetUInt32(index);
        }

        public override long GetInt64(int ordinal)
        {
            return _reader.GetInt64(ordinal);
        }

        public ulong GetUInt64(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? ulong.MinValue : _reader.GetUInt64(index);
        }

        public override string GetString(int ordinal)
        {
            return _reader.GetString(ordinal);
        }

        public string GetString(string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? string.Empty : _reader.GetString(index);
        }

        public override object GetValue(int ordinal)
        {
            return _reader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        public sbyte GetSByte(int ordinal)
        {
            return _reader.GetSByte(ordinal);
        }

        public sbyte GetSByte(string name)
        {
            return _reader.GetSByte(name);
        }

        public Guid GetGuid (string name)
        {
            int index = _reader.GetOrdinal(name);
            return _reader.IsDBNull(index) ? Guid.Empty : _reader.GetGuid(index);
        }

        public decimal GetDecimal(string name)
        {
            return _reader.GetDecimal(name);
        }

        #endregion // field falues

        public override string GetName(int ordinal)
        {
            return _reader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        public override DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }


        public override bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        public override bool NextResult()
        {
            return _reader.NextResult();
        }

        public override bool Read()
        {
            return _reader.Read();
        }
    }
}
