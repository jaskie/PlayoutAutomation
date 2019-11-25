using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Svt.Caspar
{
	public class CGManager
	{
        internal Channel Channel { get; private set; }
       
        internal CGManager(Channel channel)
		{
			Channel = channel;
		}

        public void Add(int layer, string template)
		{
            Add(layer, template, false, string.Empty);
		}
        public void Add(int videoLayer, int layer, string template)
        {
            Add(videoLayer, layer, template, false, string.Empty);
        }
        public void Add(int layer, string template, bool bPlayOnLoad)
		{
            Add(layer, template, bPlayOnLoad, string.Empty);
		}
        public void Add(int videoLayer, int layer, string template, bool bPlayOnLoad)
        {
            Add(videoLayer, layer, template, bPlayOnLoad, string.Empty);
        }
        public void Add(int layer, string template, string data)
		{
            Add(layer, template, false, data);
		}
        public void Add(int videoLayer, int layer, string template, string data)
        {
            Add(videoLayer, layer, template, false, data);
        }
        public void Add(int layer, string template, bool bPlayOnLoad, string data)
		{
            Channel.Connection.SendString("CG " + Channel.Id + " ADD " + layer + " \"" + template + "\" " + (bPlayOnLoad ? "1" : "0") + " \"" + (data ?? string.Empty) + "\"");
		}
        public void Add(int videoLayer, int layer, string template, bool bPlayOnLoad, string data)
        {
            if (videoLayer == -1)
                Add(layer, template, bPlayOnLoad, data);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " ADD " + layer + " \"" + template + "\" " + (bPlayOnLoad ? "1" : "0") + " \"" + (data ?? string.Empty) + "\"");
        }

		public void Add(int layer, string template, ICGDataContainer data)
		{
			Add(layer, template, false, data);
		}
        public void Add(int videoLayer, int layer, string template, ICGDataContainer data)
        {
            Add(videoLayer, layer, template, false, data);
        }
        public void Add(int layer, string template, bool bPlayOnLoad, ICGDataContainer data)
        {
            Channel.Connection.SendString("CG " + Channel.Id + " ADD " + layer + " \"" + template + "\" " + (bPlayOnLoad ? "1" : "0") + " \"" + (data?.ToAMCPEscapedXml() ?? string.Empty) + "\"");
        }
		public void Add(int videoLayer, int layer, string template, bool bPlayOnLoad, ICGDataContainer data)
		{
            if (videoLayer == -1)
                Add(layer, template, bPlayOnLoad, data);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " ADD " + layer + " \"" + template + "\" " + (bPlayOnLoad ? "1" : "0") + " \"" + (data?.ToAMCPEscapedXml() ?? string.Empty) + "\"");
		}







        public void Remove(int layer)
		{
            Channel.Connection.SendString("CG " + Channel.Id + " REMOVE " + layer);
		}
        public void Remove(int videoLayer, int layer)
        {
            if (videoLayer == -1)
                Remove(layer);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " REMOVE " + layer);
        }





		
		public void Clear()
		{
			Channel.Connection.SendString("CG " + Channel.Id + " CLEAR");
		}
        public void Clear(int videoLayer)
        {
            if (videoLayer == -1)
                Clear();
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " CLEAR");
        }






        public void Play(int layer)
		{
            Channel.Connection.SendString("CG " + Channel.Id + " PLAY " + layer);
		}
        public void Play(int videoLayer, int layer)
        {
            if (videoLayer == -1)
                Play(layer);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " PLAY " + layer);
        }






        public void Stop(int layer)
		{
            Channel.Connection.SendString("CG " + Channel.Id + " STOP " + layer);
		}
        public void Stop(int videoLayer, int layer)
        {
            if (videoLayer == -1)
                Stop(layer);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " STOP " + layer);
        }


        public void Next(int layer)
		{
            Channel.Connection.SendString("CG " + Channel.Id + " NEXT " + layer);
		}
        public void Next(int videoLayer, int layer)
        {
            if (videoLayer == -1)
                Next(layer);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " NEXT " + layer);
        }

       
	    public void Update(int layer, string data)
	    {
	        Channel.Connection.SendString("CG " + Channel.Id + " UPDATE " + layer + " " + " \"" + data + "\"");
	    }
	    public void Update(int videoLayer, int layer, string data)
	    {
	        if (videoLayer == -1)
	            Update(layer, data);
	        else
	            Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " UPDATE " + layer + " " + " \"" + data + "\"");
	    }

        
        public void Invoke(int layer, string method)
		{
			Channel.Connection.SendString("CG " + Channel.Id + " INVOKE " + layer + " " + method);
		}
        public void Invoke(int videoLayer, int layer, string method)
        {
            if (videoLayer == -1)
                Invoke(layer, method);
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " INVOKE " + layer + " " + method);
        }


        public void Info()
		{
			Channel.Connection.SendString("CG " + Channel.Id + " INFO");
		}
        public void Info(int videoLayer)
        {
            if (videoLayer == -1)
                Info();
            else
                Channel.Connection.SendString("CG " + Channel.Id + "-" + videoLayer + " INFO");
        }
	}
    	
	public interface ICGDataContainer
	{
		string ToXml();
		string ToAMCPEscapedXml();
	}

	public class CasparCGDataCollection : ICGDataContainer
	{
		private Dictionary<string, ICGComponentData> data_ = new Dictionary<string, ICGComponentData>();

		public void SetData(string name, string value)
		{
			data_[name] = new CGTextFieldData(value);
		}
		public void SetData(string name, ICGComponentData data)
		{
			data_[name] = data;
		}
		public ICGComponentData GetData(string name)
		{
			if (!string.IsNullOrEmpty(name) && data_.ContainsKey(name))
				return data_[name];

			return null;
		}
		public void Clear()
		{
			data_.Clear();
		}
		public void RemoveData(string name)
		{
			if(!string.IsNullOrEmpty(name) && data_.ContainsKey(name))
				data_.Remove(name);
		}

        public List<CGDataPair> DataPairs
        {
            get
            {
                List<CGDataPair> dataPairs = new List<CGDataPair>();
                data_.ToList().ForEach(d => dataPairs.Add(new CGDataPair(d.Key, d.Value)));

                return dataPairs;
            }
        }

		public string ToXml()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<templateData>");
			foreach (string key in data_.Keys)
			{
				sb.Append("<componentData id=\"" + key + "\">");
				data_[key].ToXml(sb);
				sb.Append("</componentData>");
			}
			sb.Append("</templateData>");
			return sb.ToString();
		}

		public string ToAMCPEscapedXml()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<templateData>");
			foreach(string key in data_.Keys)
			{
				sb.Append("<componentData id=\\\"" + key + "\\\">");
				data_[key].ToAMCPEscapedXml(sb);
				sb.Append("</componentData>");
			}
			sb.Append("</templateData>");

			sb.Replace(Environment.NewLine, "\\n");
			return sb.ToString();
		}
	}
}
