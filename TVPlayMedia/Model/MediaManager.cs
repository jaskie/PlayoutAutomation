using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class MediaManager : ProxyBase, IMediaManager
    {
        public void ArchiveMedia(IEnumerable<IServerMedia> mediaList, bool deleteAfter)
        {
            Invoke(parameters: new object[] { mediaList, deleteAfter });
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList)
        {
            return Query<List<MediaDeleteDenyReason>>(parameters: mediaList);
        }

        public void Export(IEnumerable<ExportMedia> exportList, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { exportList, directory });
        }

        public IAnimationDirectory AnimationDirectoryPRI { get { return Get<AnimationDirectory>(); } set { SetField(value); } }
        public IAnimationDirectory AnimationDirectorySEC { get { return Get<AnimationDirectory>(); } set { SetField(value); } }
        public IAnimationDirectory AnimationDirectoryPRV { get { return Get<AnimationDirectory>(); } set { SetField(value); } }

        public IArchiveDirectory ArchiveDirectory { get { return Get<ArchiveDirectory>(); } set { SetField(value); } }

        public IEngine getEngine()
        {
            throw new NotImplementedException();
        }

        public IFileManager FileManager { get { return Get<IFileManager>(); }  set { SetField(value); } }

        public VideoFormatDescription FormatDescription { get { return Get<VideoFormatDescription>(); } set { SetField(value); } }

        public List<IIngestDirectory> IngestDirectories
        {
            get { return Get<List<IngestDirectory>>().Cast<IIngestDirectory>().ToList(); }
            set { SetField(value); }
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            Invoke(parameters: mediaList);
        }

        public IServerDirectory MediaDirectoryPRI { get { return Get<ServerDirectory>(); } set { SetField(value); } }
        public IServerDirectory MediaDirectorySEC { get { return Get<ServerDirectory>(); } set { SetField(value); } }
        public IServerDirectory MediaDirectoryPRV { get { return Get<ServerDirectory>(); } set { SetField(value); } }

        public IMedia GetPRVMedia(IMedia media)
        {
            return Query<Media>(parameters: media);
        }

        public TVideoFormat VideoFormat { get { return Get<TVideoFormat>(); } set { SetField(value); } }

        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList, bool toTop) { Invoke(parameters: new object[] { mediaList, toTop }); }
        
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void ReloadIngestDirs()
        {
            Invoke();
        }

        public void SynchronizeSecToPri(bool deleteNotExisted)
        {
            Invoke(parameters: deleteNotExisted);
        }

        public void Export(IEnumerable<ExportMedia> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { exportList, asSingleFile, singleFilename, directory });
        }
        
    }
}
