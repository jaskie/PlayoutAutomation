namespace TAS.Common.Database.Interfaces.Media
{
    public interface IPersistentMedia: Common.Interfaces.Media.IPersistentMedia 
    {
        ulong IdPersistentMedia { get; set; }
        new string FileName { get; set; }
    }
}