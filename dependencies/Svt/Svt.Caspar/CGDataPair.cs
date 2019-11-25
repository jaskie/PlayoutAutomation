using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
    public class CGDataPair
    {
        public CGDataPair(string name, ICGComponentData data)
        {
            Name = name;
            Data = data;
        }
        public CGDataPair(string name, string value)
        {
            Name = name;
            Value = value;
        }

        private string name_;
        public string Name
        {
            get { return name_; }
            set { name_ = value; }
        }

        public string Value
        {
            get { return (Data != null) ? Data.ToString() : string.Empty; }
            set
            {
                if (value == null)
					Data = new CGTextFieldData(string.Empty);
                else
                    Data = new CGTextFieldData(value);
            }
        }

        private ICGComponentData data_;
        public ICGComponentData Data
        {
            get { return data_; }
            set { data_ = value; }
        }

        private void ToEscapedXml(StringBuilder sb)
        {
            StringBuilder datasb = new StringBuilder();
            Data.ToXml(datasb);
            datasb.Replace("\"", "\\\"");
            
            sb.Append("<componentData id=\\\"" + Name + "\\\">");
            sb.Append(datasb.ToString());
            sb.Append("</componentData>");

        }
        private void ToXml(StringBuilder sb)
        {
			if (Data != null)
			{
				sb.Append("<componentData id=\"" + Name + "\">");
				Data.ToXml(sb);
				sb.Append("</componentData>");
			}
        }


        public static string ToXml(IEnumerable<CGDataPair> pairs)
        {
            StringBuilder result = new StringBuilder("<templateData>");
            foreach (CGDataPair pair in pairs)
            {
                pair.ToXml(result);
            }
            result.Append("</templateData>");
            return result.ToString();
        }
        public static string ToEscapedXml(IEnumerable<CGDataPair> pairs)
        {
            StringBuilder result = new StringBuilder("<templateData>");
            foreach (CGDataPair pair in pairs)
            {
                pair.ToEscapedXml(result);
            }
            result.Append("</templateData>");
            return result.ToString();
        }
    }
}
