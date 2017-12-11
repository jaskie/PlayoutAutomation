using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
	public class CGXmlData : Svt.Caspar.ICGComponentData
	{
		public CGXmlData(string xmlString)
		{
			XmlString = xmlString;
		}

		public string XmlString { get; set; }
		#region ICGComponentData Members

		public void ToXml(StringBuilder sb)
		{
			sb.Append("<data id=\"xml\">");
			sb.Append(XmlString);
			sb.Append("</data>");
		}

		public void ToAMCPEscapedXml(StringBuilder sb)
		{
			sb.Append("<data id=\\\"xml\\\">");
			string escapedXml = XmlString.Replace("\\", "\\\\");
			escapedXml = escapedXml.Replace("\"", "\\\"");
			sb.Append(escapedXml);
			sb.Append("</data>");
		}


		#endregion
	}
}
