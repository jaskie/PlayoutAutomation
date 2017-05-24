namespace TAS.Server.Common.Interfaces
{
    public interface IXdcamMedia: IIngestMedia
    {
        int ClipNr { get; }
    }
}
