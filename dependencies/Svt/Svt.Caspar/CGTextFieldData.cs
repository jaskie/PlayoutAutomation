using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
    public class CGTextFieldData : ICGComponentData
    {
        public CGTextFieldData(string data)
        {
            Data = data;
        }

        public string data_;
        public string Data
        {
            get { return data_; }
            set { data_ = value; }
        }

        public void ToAMCPEscapedXml(StringBuilder sb)
        {
            sb.Append("<data id=\\\"text\\\" value=\\\"");
            string escapedValue = string.IsNullOrEmpty(Data) ? string.Empty : Data.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\\", "\\\\");
            sb.Append(escapedValue);
            sb.Append("\\\" />");
        }

        public void ToXml(StringBuilder sb)
        {
            string value = (Data != null) ? Data : string.Empty;
            string escapedValue = value.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
            sb.Append("<data id=\"text\" value=\"" + escapedValue + "\" />");
        }

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Data)) ? string.Empty : Data;
        }
    }
}
