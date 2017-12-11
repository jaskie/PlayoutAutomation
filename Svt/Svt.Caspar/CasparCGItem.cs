using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.XPath;

namespace Svt.Caspar
{
	[Obsolete("use ICGDataContainer for CGData instead", false)]
	public class CasparCGItem : System.Xml.Serialization.IXmlSerializable
	{
		public CasparCGItem()
		{
		}
		
		public CasparCGItem(string templateName)
		{
			TemplateName = templateName;
		}

		public CasparCGItem(string templateName, string templateProfile)
		{
			TemplateName = templateName;
			TemplateProfile = templateProfile;
		}
		public CasparCGItem(string templateName, int layer, bool autoPlay)
		{
			TemplateName = templateName;
            Layer = layer;
			AutoPlay = autoPlay;
		}
        public CasparCGItem(string templateName, string templateProfile, int layer)
		{
			TemplateName = templateName;
			TemplateProfile = templateProfile;
            Layer = layer;
		}
		
		private string templateName_ = "";
		public string TemplateName
		{
			get { return templateName_; }
			set { templateName_ = (value != null) ? value : string.Empty; }
		}
		private string templateProfile_ = "";
		public string TemplateProfile
		{
			get { return templateProfile_; }
			set { templateProfile_ = (value != null) ? value : string.Empty; }
		}

		public string TemplateIdentifier
		{
			get
			{
				if (templateProfile_ != string.Empty)
					return templateProfile_ + '/' + templateName_;
				return templateName_;
			}
		}

        private int layer_ = 0;
		public int Layer
		{
            get { return layer_; }
            set { layer_ = value; }
		}
        private int videoLayer_ = -1;
        public int VideoLayer
        {
            get { return videoLayer_; }
            set { videoLayer_ = value; }
        }
		private bool bAutoPlay_ = false;
		public bool AutoPlay 
		{
			get { return bAutoPlay_; }
			set { bAutoPlay_ = value; }
		}

		private List<CGDataPair> data_ = new List<CGDataPair>();
		public List<CGDataPair> Data
		{
			get { return data_; }
		}
		internal string XMLData
		{
			get 
			{
				return CGDataPair.ToEscapedXml(data_);
			}
		}

		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			reader.MoveToContent();
			string templatename = reader["templatename"];
			if (!string.IsNullOrEmpty(templatename))
				TemplateName = templatename;

			string templatefolder = reader["templatefolder"];
			if (!string.IsNullOrEmpty(templatefolder))
				TemplateProfile = templatefolder;

			string layerString = reader["layer"];
			if (!string.IsNullOrEmpty(layerString))
				Layer = Int32.Parse(layerString);

            string videoLayerString = reader["videoLayer"];
            if (!string.IsNullOrEmpty(videoLayerString))
                VideoLayer = Int32.Parse(videoLayerString);

			XPathDocument xpDoc = new XPathDocument(reader);
			XPathNavigator nav = xpDoc.CreateNavigator();
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(nav.NameTable);
			nsmgr.AddNamespace("cp", Properties.Resources.CasparPlayoutSchemaURL);

			//Read data
			Data.Clear();
			XPathNodeIterator it = nav.Select("/cp:cgitem/cp:data/cp:string", nsmgr);
			while (it.MoveNext())
			{
				string value = (string)it.Current.Evaluate("string(text())", nsmgr);
				string name = (string)it.Current.Evaluate("string(@id)", nsmgr);
				Data.Add(new CGDataPair(name, value));
			}

		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("cgitem", Properties.Resources.CasparPlayoutSchemaURL);
			writer.WriteAttributeString("templatename", TemplateName);
			writer.WriteAttributeString("templatefolder", TemplateProfile);
			writer.WriteAttributeString("layer", Layer.ToString());
            writer.WriteAttributeString("videoLayer", VideoLayer.ToString());
			if (Data.Count > 0)
			{
				writer.WriteStartElement("data", Properties.Resources.CasparPlayoutSchemaURL);
				foreach(CGDataPair pair in Data)
				{
					writer.WriteStartElement("string", Properties.Resources.CasparPlayoutSchemaURL);
					writer.WriteAttributeString("id", pair.Name);
					writer.WriteString(pair.Value);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		#endregion
	}

}
