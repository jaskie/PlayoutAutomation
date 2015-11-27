using System;
using System.ComponentModel;
using System.IO;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMedia: IMediaProperties, INotifyPropertyChanged, IDto
    {
        IMediaDirectory Directory { get; }
        bool FileExists();
        bool Delete();
        void ReVerify();
        RationalNumber FrameRate { get; }
        void GetLoudness();
    }
}
