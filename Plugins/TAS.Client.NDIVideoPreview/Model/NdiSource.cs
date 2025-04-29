using System;

namespace TAS.Client.NDIVideoPreview.Model
{
    public class NdiSource
    {

        public NdiSource(string displayName)
        {
            DisplayName = displayName;
        }

        public NdiSource(string name, string address)
        {
            DisplayName = name;
            SourceName = name;
            Address = address;
        }

        public string SourceName { get; }
        
        public string DisplayName { get; }

        public string Address { get; set; }
    }


    internal class NdiSourceEventArgs : EventArgs
    {
        public NdiSourceEventArgs(NdiSource source)
        {
            Source = source;
        }
        public NdiSource Source { get; }
    }
}
