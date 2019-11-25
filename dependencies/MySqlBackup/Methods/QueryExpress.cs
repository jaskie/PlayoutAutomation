using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;

namespace MySql.Data.MySqlClient
{
    public class QueryExpress
    {
        static NumberFormatInfo _numberFormatInfo = new NumberFormatInfo()
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = string.Empty
        };

        static DateTimeFormatInfo _dateFormatInfo = new DateTimeFormatInfo()
        {
            DateSeparator = "-",
            TimeSeparator = ":"
        };

        public static NumberFormatInfo MySqlNumberFormat { get { return _numberFormatInfo; } }

        public static DateTimeFormatInfo MySqlDateTimeFormat { get { return _dateFormatInfo; } }

        public static DataTable GetTable(MySqlCommand cmd, string sql)
        {
            DataTable dt = new DataTable();
            cmd.CommandText = sql;
            using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }
            return dt;
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql)
        {
            cmd.CommandText = sql;
            object ob = cmd.ExecuteScalar();
            if (ob is byte[])
                return Encoding.UTF8.GetString((byte[])ob);
            else
                return ob + "";
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql, int columnIndex)
        {
            DataTable dt = GetTable(cmd, sql);

            if (dt.Rows[0][columnIndex] is byte[])
                return Encoding.UTF8.GetString((byte[])dt.Rows[0][columnIndex]);
            else
                return dt.Rows[0][columnIndex] + "";
        }

        public static string ExecuteScalarStr(MySqlCommand cmd, string sql, string columnName)
        {
            DataTable dt = GetTable(cmd, sql);

            if (dt.Rows[0][columnName] is byte[])
                return Encoding.UTF8.GetString((byte[])dt.Rows[0][columnName]);
            else
                return dt.Rows[0][columnName] + "";
        }

        public static long ExecuteScalarLong(MySqlCommand cmd, string sql)
        {
            long l = 0;
            cmd.CommandText = sql;
            long.TryParse(cmd.ExecuteScalar() + "", out l);
            return l;
        }

        public static string EscapeStringSequence(string data)
        {
            var builder = new StringBuilder();
            foreach (var ch in data)
            {
                switch (ch)
                {
                    case '\\': // Backslash
                        builder.Append("\\\\");
                        break;
                    case '\r': // Carriage return
                        builder.Append("\\r");
                        break;
                    case '\n': // New Line
                        builder.Append("\\n");
                        break;
                    //case '\a': // Vertical tab
                    //    builder.Append("\\a");
                    //    break;
                    case '\b': // Backspace
                        builder.Append("\\b");
                        break;
                    //case '\f': // Formfeed
                    //    builder.Append("\\f");
                    //    break;
                    case '\t': // Horizontal tab
                        builder.Append("\\t");
                        break;
                    //case '\v': // Vertical tab
                    //    builder.Append("\\v");
                    //    break;
                    case '\"': // Double quotation mark
                        builder.Append("\\\"");
                        break;
                    case '\'': // Single quotation mark
                        builder.Append("''");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        public static string EraseDefiner(string input)
        {
            StringBuilder sb = new StringBuilder();
            string definer = " DEFINER=";
            int dIndex = input.IndexOf(definer);

            sb.Append(definer);

            bool pointAliasReached = false;
            bool point3rdQuoteReached = false;

            for (int i = dIndex + definer.Length; i < input.Length; i++)
            {
                if (!pointAliasReached)
                {
                    if (input[i] == '@')
                        pointAliasReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (!point3rdQuoteReached)
                {
                    if (input[i] == '`')
                        point3rdQuoteReached = true;

                    sb.Append(input[i]);
                    continue;
                }

                if (input[i] != '`')
                {
                    sb.Append(input[i]);
                    continue;
                }
                else
                {
                    sb.Append(input[i]);
                    break;
                }
            }

            return input.Replace(sb.ToString(), string.Empty);
        }

        public static string ConvertToSqlFormat(object ob, bool wrapStringWithSingleQuote, bool escapeStringSequence, MySqlColumn col)
        {
            StringBuilder sb = new StringBuilder();

            if (ob == null || ob is System.DBNull)
            {
                sb.Append("NULL");
            }
            else if (ob is System.String)
            {
                string str = (string)ob;

                if (escapeStringSequence)
                    str = QueryExpress.EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.Append("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is System.Boolean)
            {
                sb.Append(Convert.ToInt32(ob).ToString());
            }
            else if (ob is System.Byte[])
            {
                if (((byte[])ob).Length == 0)
                {
                    if (wrapStringWithSingleQuote)
                        return "''";
                    else
                        return "";
                }
                else
                {
                    sb.Append(CryptoExpress.ConvertByteArrayToHexString((byte[])ob));
                }
            }
            else if (ob is short)
            {
                sb.Append(((short)ob).ToString(_numberFormatInfo));
            }
            else if (ob is int)
            {
                sb.Append(((int)ob).ToString(_numberFormatInfo));
            }
            else if (ob is long)
            {
                sb.Append(((long)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ushort)
            {
                sb.Append(((ushort)ob).ToString(_numberFormatInfo));
            }
            else if (ob is uint)
            {
                sb.Append(((uint)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ulong)
            {
                sb.Append(((ulong)ob).ToString(_numberFormatInfo));
            }
            else if (ob is double)
            {
                sb.Append(((double)ob).ToString(_numberFormatInfo));
            }
            else if (ob is decimal)
            {
                sb.Append(((decimal)ob).ToString(_numberFormatInfo));
            }
            else if (ob is float)
            {
                sb.Append(((float)ob).ToString(_numberFormatInfo));
            }
            else if (ob is byte)
            {
                sb.Append(((byte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is sbyte)
            {
                sb.Append(((sbyte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)ob;

                if (wrapStringWithSingleQuote)
                    sb.Append("'");

                sb.Append(ts.Hours.ToString("D2"))
                  .Append(":")
                  .Append(ts.Minutes.ToString("D2"))
                  .Append(":")
                  .Append(ts.Seconds.ToString("D2"))
                  .Append(".")
                  .Append(ts.Milliseconds.ToString("D3"));

                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.Append("'");
               
                sb.Append(((DateTime)ob).ToString("yyyy-MM-dd HH:mm:ss.fff", _dateFormatInfo));

                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is MySql.Data.Types.MySqlDateTime)
            {
                MySql.Data.Types.MySqlDateTime mdt = (MySql.Data.Types.MySqlDateTime)ob;

                if (mdt.IsNull)
                {
                    sb.Append("NULL");
                }
                else
                {
                    if (mdt.IsValidDateTime)
                    {
                        DateTime dtime = mdt.Value;

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");

                        if (col.MySqlDataType == "datetime")
                            sb.Append(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));
                        else if (col.MySqlDataType == "date")
                            sb.Append(dtime.ToString("yyyy-MM-dd", _dateFormatInfo));
                        else if (col.MySqlDataType == "time")
                            sb.Append(dtime.ToString("HH:mm:ss", _dateFormatInfo));
                        else
                            sb.Append(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));

                        if (dtime.Millisecond > 0)
                        {
                            sb.Append(".");
                            sb.Append(dtime.Millisecond.ToString("D3"));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");
                    }
                    else
                    {
                        if (wrapStringWithSingleQuote)
                            sb.Append("'");

                        if (col.MySqlDataType == "datetime")
                            sb.Append("0000-00-00 00:00:00");
                        else if (col.MySqlDataType == "date")
                            sb.Append("0000-00-00");
                        else if (col.MySqlDataType == "time")
                            sb.Append("00:00:00");
                        else
                            sb.Append("0000-00-00 00:00:00");

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");
                    }
                }
            }
            else if (ob is System.Guid)
            {
                if (col.MySqlDataType == "binary(16)")
                {
                    sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid)ob).ToByteArray()));
                }
                else if (col.MySqlDataType == "char(36)")
                {
                    if (wrapStringWithSingleQuote)
                        sb.Append("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.Append("'");
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.Append("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.Append("'");
                }
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType().ToString() + ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }

        public static string ConvertToSqlFormat(MySqlDataReader rdr, int colIndex, bool wrapStringWithSingleQuote, bool escapeStringSequence, MySqlColumn col)
        {
            object ob = rdr[colIndex];

            StringBuilder sb = new StringBuilder();

            if (ob == null || ob is System.DBNull)
            {
                sb.Append("NULL");
            }
            else if (ob is System.String)
            {
                string str = (string)ob;

                if (escapeStringSequence)
                    str = QueryExpress.EscapeStringSequence(str);

                if (wrapStringWithSingleQuote)
                    sb.Append("'");

                sb.Append(str);

                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is System.Boolean)
            {
                sb.Append(Convert.ToInt32(ob).ToString());
            }
            else if (ob is System.Byte[])
            {
                if (((byte[])ob).Length == 0)
                {
                    if (wrapStringWithSingleQuote)
                        return "''";
                    else
                        return "";
                }
                else
                {
                    sb.Append(CryptoExpress.ConvertByteArrayToHexString((byte[])ob));
                }
            }
            else if (ob is short)
            {
                sb.Append(((short)ob).ToString(_numberFormatInfo));
            }
            else if (ob is int)
            {
                sb.Append(((int)ob).ToString(_numberFormatInfo));
            }
            else if (ob is long)
            {
                sb.Append(((long)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ushort)
            {
                sb.Append(((ushort)ob).ToString(_numberFormatInfo));
            }
            else if (ob is uint)
            {
                sb.Append(((uint)ob).ToString(_numberFormatInfo));
            }
            else if (ob is ulong)
            {
                sb.Append(((ulong)ob).ToString(_numberFormatInfo));
            }
            else if (ob is double)
            {
                sb.Append(((double)ob).ToString(_numberFormatInfo));
            }
            else if (ob is decimal)
            {
                sb.Append(((decimal)ob).ToString(_numberFormatInfo));
            }
            else if (ob is float)
            {
                sb.Append(((float)ob).ToString(_numberFormatInfo));
            }
            else if (ob is byte)
            {
                sb.Append(((byte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is sbyte)
            {
                sb.Append(((sbyte)ob).ToString(_numberFormatInfo));
            }
            else if (ob is TimeSpan)
            {
                TimeSpan ts = (TimeSpan)ob;

                if (wrapStringWithSingleQuote)
                    sb.Append("'");

                sb.Append(ts.Hours.ToString("D2"))
                    .Append(":")
                    .Append(ts.Minutes.ToString("D2"))
                    .Append(":")
                    .Append(ts.Seconds.ToString("D2"))
                    .Append(".")
                    .Append(ts.Milliseconds.ToString("D3"));
                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is System.DateTime)
            {
                if (wrapStringWithSingleQuote)
                    sb.Append("'");
                sb.Append(((DateTime)ob).ToString("yyyy-MM-dd HH:mm:ss.fff", _dateFormatInfo));
                if (col.TimeFractionLength > 0)
                {
                    sb.Append(".");
                    string _millisecond = rdr.GetMySqlDateTime(colIndex).Millisecond.ToString();
                    if (_millisecond.Length < col.TimeFractionLength)
                    {
                        _millisecond = _millisecond.PadLeft(col.TimeFractionLength, '0');
                    }
                    else if (_millisecond.Length > col.TimeFractionLength)
                    {
                        _millisecond = _millisecond.Substring(0, col.TimeFractionLength);
                    }
                    sb.Append(_millisecond.ToString().PadLeft(col.TimeFractionLength, '0'));
                }

                if (wrapStringWithSingleQuote)
                    sb.Append("'");
            }
            else if (ob is MySql.Data.Types.MySqlDateTime)
            {
                MySql.Data.Types.MySqlDateTime mdt = (MySql.Data.Types.MySqlDateTime)ob;

                if (mdt.IsNull)
                {
                    sb.Append("NULL");
                }
                else
                {
                    if (mdt.IsValidDateTime)
                    {
                        DateTime dtime = mdt.Value;

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");

                        if (col.MySqlDataType == "datetime")
                            sb.Append(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));
                        else if (col.MySqlDataType == "date")
                            sb.Append(dtime.ToString("yyyy-MM-dd", _dateFormatInfo));
                        else if (col.MySqlDataType == "time")
                            sb.Append(dtime.ToString("HH:mm:ss", _dateFormatInfo));
                        else
                            sb.Append(dtime.ToString("yyyy-MM-dd HH:mm:ss", _dateFormatInfo));

                        if(col.TimeFractionLength > 0)
                        {
                            sb.Append(".");
                            sb.Append(((MySql.Data.Types.MySqlDateTime)ob).Millisecond.ToString().PadLeft(col.TimeFractionLength, '0'));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");
                    }
                    else
                    {
                        if (wrapStringWithSingleQuote)
                            sb.Append("'");

                        if (col.MySqlDataType == "datetime")
                            sb.Append("0000-00-00 00:00:00");
                        else if (col.MySqlDataType == "date")
                            sb.Append("0000-00-00");
                        else if (col.MySqlDataType == "time")
                            sb.Append("00:00:00");
                        else
                            sb.Append("0000-00-00 00:00:00");

                        if (col.TimeFractionLength > 0)
                        {
                            sb.Append(".".PadRight(col.TimeFractionLength, '0'));
                        }

                        if (wrapStringWithSingleQuote)
                            sb.Append("'");
                    }
                }
            }
            else if (ob is System.Guid)
            {
                if (col.MySqlDataType == "binary(16)")
                {
                    sb.Append(CryptoExpress.ConvertByteArrayToHexString(((Guid)ob).ToByteArray()));
                }
                else if (col.MySqlDataType == "char(36)")
                {
                    if (wrapStringWithSingleQuote)
                        sb.Append("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.Append("'");
                }
                else
                {
                    if (wrapStringWithSingleQuote)
                        sb.Append("'");

                    sb.Append(ob);

                    if (wrapStringWithSingleQuote)
                        sb.Append("'");
                }
            }
            else
            {
                throw new Exception("Unhandled data type. Current processing data type: " + ob.GetType().ToString() + ". Please report this bug with this message to the development team.");
            }
            return sb.ToString();
        }
    }
}