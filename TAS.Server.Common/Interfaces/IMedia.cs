using System;
using System.ComponentModel;
using System.IO;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IMedia: IMediaProperties, INotifyPropertyChanged
    {
        IMediaDirectory Directory { get; }
        bool FileExists();
        bool FilePropertiesEqual(IMedia m);
        bool Delete();
        void Verify();
        void Remove();
        bool CopyMediaTo(IMedia destMedia, ref bool abortCopy);
        Stream GetFileStream(bool forWrite);
        void CloneMediaProperties(IMedia from);
        RationalNumber FrameRate { get; }
    }
}
