using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Database.Interfaces.Media
{
    public interface IPersistentMedia: Common.Interfaces.Media.IPersistentMedia 
    {
        ulong IdPersistentMedia { get; set; }
        new string FileName { get; set; }
        void DisableIsModified();
        void EnableIsModified();
    }
}