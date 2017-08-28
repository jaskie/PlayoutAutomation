namespace TAS.Common.Interfaces
{
    public interface ICGElementsState
    {
        bool IsCGEnabled { get; set; }
        byte Crawl { get; set; } // 0 - none
        byte Logo { get; set; }
        byte Parental { get; set; }
    }
}
