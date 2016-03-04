using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    [System.ServiceModel.ServiceContract]
    public interface IServerDirectory: IMediaDirectory
    {
        IPlayoutServer Server { get; }
        IServerMedia GetServerMedia(IMedia media, bool searchExisting = true);
    }
}
