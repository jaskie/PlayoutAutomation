using System;
using System.ComponentModel;
using System.IO;
using TAS.Common;
using TAS.Remoting;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMedia: IMediaProperties, INotifyPropertyChanged, IDto
    {
        IMediaDirectory Directory { get; }
        string FullPath { get; }
        bool FileExists();
        bool Delete();
        bool IsVerified { get; set; }
        void ReVerify();
        void Verify();
        RationalNumber FrameRate { get; }
        void GetLoudness();
        VideoFormatDescription VideoFormatDescription { get; }
    }
}
