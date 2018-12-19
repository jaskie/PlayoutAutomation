namespace TAS.Common.Interfaces.Media
{
    public interface IXdcamMedia: IIngestMedia
    {
        int ClipNr { get; }
    }
}
