using TAS.Common.Interfaces;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPreviewProvider
    {
        IUiPreview Preview { get; }
        IEngine Engine { get; }
    }
}
