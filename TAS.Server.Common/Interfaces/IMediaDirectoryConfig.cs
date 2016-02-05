using System;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectoryConfig
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
    }
}
