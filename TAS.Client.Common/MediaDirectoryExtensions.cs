using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Common
{
    public static class MediaDirectoryExtensions
    {
        public static string GetDisplayName(this IMediaDirectory mediaDirectory)
        {
            switch (mediaDirectory)
            {
                case IServerDirectory serverDirectory:
                    return serverDirectory.IsPrimary ? Properties.Resources._primary : Properties.Resources._secondary;
                case IAnimationDirectory animationDirectory:
                    return animationDirectory.IsPrimary ? Properties.Resources._animations_Primary : Properties.Resources._animations_Secondary;
                case IIngestDirectory ingestDirectory:
                    return ingestDirectory.DirectoryName;
                case IArchiveDirectory _:
                    return Properties.Resources._archive;
                default:
                    return mediaDirectory.Folder;
            }
        }
    }
}
