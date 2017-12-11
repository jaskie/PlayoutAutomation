using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Caspar
{
	public enum MediaType
	{
		STILL,
		MOVIE,
		AUDIO
	}

	public class MediaInfo
	{
		internal MediaInfo(string folder, string name, MediaType type, Int64 size, DateTime updated)
		{
			Folder = folder;
			Name = name;
			Size = size;
			LastUpdated = updated;
			Type = type;
		}

		private string folder_;
		public string Folder
		{
			get { return folder_; }
			internal set { folder_ = value; }
		}
		private string name_;
		public string Name
		{
			get { return name_; }
			internal set { name_ = value; }
		}
		public string FullName
		{
			get
			{
				if (!String.IsNullOrEmpty(Folder))
					return Folder + '\\' + Name;
				else
					return Name;
			}
		}
		private MediaType type_;
		public MediaType Type
		{
			get { return type_; }
			set { type_ = value; }
		}

		private Int64 size_;
		public Int64 Size
		{
			get { return size_; }
			internal set { size_ = value; }
		}

		private DateTime updated_;
		public DateTime LastUpdated
		{
			get { return updated_; }
			internal set { updated_ = value; }
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}
