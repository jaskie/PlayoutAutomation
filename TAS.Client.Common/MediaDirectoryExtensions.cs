using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Common
{
    public static class MediaDirectoryExtensions
    {
        public static string GetDisplayName(this IMediaDirectory mediaDirectory, IMediaManager mediaManager)
        {
            switch (mediaDirectory)
            {
                case IServerDirectory _:
                    return mediaDirectory == mediaManager.MediaDirectoryPRI ? Properties.Resources._primary : Properties.Resources._secondary;
                case IAnimationDirectory _:
                    return mediaDirectory == mediaManager.AnimationDirectoryPRI ? Properties.Resources._animations_Primary : Properties.Resources._animations_Secondary;
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
