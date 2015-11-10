using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using WebSocketSharp;

namespace TAS.Client.Model
{
    public class MediaManager: IMediaManager
    {
        readonly string _address;
        WebSocket _clientSocket;
        
        public MediaManager(string host)
        {
            _address = host;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public void Initialize()
        {
            try
            {
                _clientSocket = new WebSocket(string.Format("ws://{0}/MediaManager", _address));
                _clientSocket.Connect();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "Error initializing MediaManager remote interface");
            }
        }

        public void IngestMediaToPlayout(IMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IIngestMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IArchiveMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IEnumerable<IIngestMedia> mediaList, bool ToTop = false)
        {
            throw new NotImplementedException();
        }

        public void Queue(IFileOperation operation, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void ArchiveMedia(IMedia media, bool deleteAfter)
        {
            throw new NotImplementedException();
        }

        public void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory)
        {
            throw new NotImplementedException();
        }

        public void Export(MediaExport export, IIngestDirectory directory)
        {
            throw new NotImplementedException();
        }

        public MediaDeleteDenyReason DeleteMedia(IMedia media)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList)
        {
            throw new NotImplementedException();
        }

        public void ReloadIngestDirs()
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
        {
            throw new NotImplementedException();
        }

        public IEngine getEngine()
        {
            throw new NotImplementedException();
        }

        public IAnimationDirectory getAnimationDirectoryPGM()
        {
            throw new NotImplementedException();
        }

        public IAnimationDirectory getAnimationDirectoryPRV()
        {
            throw new NotImplementedException();
        }

        public IServerDirectory getMediaDirectoryPGM()
        {
            return null;
        }

        public IServerDirectory getMediaDirectoryPRV()
        {
            throw new NotImplementedException();
        }

        public IArchiveDirectory getArchiveDirectory()
        {
            throw new NotImplementedException();
        }

        public List<IIngestDirectory> getIngestDirectories()
        {
            throw new NotImplementedException();
        }

        public ObservableSynchronizedCollection<ITemplate> getTemplates()
        {
            throw new NotImplementedException();
        }

        public IFileManager getFileManager()
        {
            return null;
        }

        public List<IMediaDirectory> getDirectories()
        {
            return null;
        }

        public VideoFormatDescription getFormatDescription()
        {
            throw new NotImplementedException();
        }

        public TVideoFormat getVideoFormat()
        {
            throw new NotImplementedException();
        }
    }


}
