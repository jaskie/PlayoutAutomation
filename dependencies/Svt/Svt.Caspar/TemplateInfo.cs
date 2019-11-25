using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
	public class TemplateInfo : ICloneable
	{
		internal TemplateInfo(string folder, string name, Int64 size, DateTime updated)
		{
			Folder = folder;
			Name = name;
			Size = size;
			LastUpdated = updated;
		}

        public string Folder { get; internal set; }
        public string Name { get; internal set; }
        public Int64 Size { get; internal set; }
        public DateTime LastUpdated { get; internal set; }

        public string FullName { get { return (Folder.Length > 0) ? (Folder + "/" + Name) : (Name); } }

		public override string ToString()
		{
			return Name;
		}
	
        public object Clone()
        {
            return new TemplateInfo(Folder, Name, Size, LastUpdated);
        }
    }
}
