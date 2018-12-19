using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server
{
    internal class MediaPropertyChangedEventArgs : MediaEventArgs
    {
        public MediaPropertyChangedEventArgs(IMedia media, string propertyName) : base(media)
        {
            PropertyName = propertyName;
        }
        public string PropertyName { get; }
    }
}
