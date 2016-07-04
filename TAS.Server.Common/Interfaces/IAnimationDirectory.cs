using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IAnimationDirectory: IMediaDirectory
    {
        IAnimatedMedia CloneMedia(IAnimatedMedia source, Guid newMediaGuid);
        event PropertyChangedEventHandler MediaPropertyChanged;
    }
}
