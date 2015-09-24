using System;

namespace TAS.Server.Interfaces
{
    public interface IMediaDirectory
    {
        string DirectoryName { get; set; }
        string Folder { get; set; }
        string Username { get; set; }
        string Password { get; set; }
    }
}
