namespace TAS.Database.Common.Interfaces.Media
{
    public interface IPersistentMedia: TAS.Common.Interfaces.Media.IPersistentMedia 
    {
        ulong IdPersistentMedia { get; set; }
        new string FileName { get; set; }
        void DisableIsModified();
        void EnableIsModified();
    }
}