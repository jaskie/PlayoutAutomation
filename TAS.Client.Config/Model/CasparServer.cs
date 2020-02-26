using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServer: IPlayoutServerProperties
    {
        public bool IsNew = true;
        [Hibernate]
        public string ServerAddress { get; set; }
        [Hibernate]
        public int OscPort { get; set; }
        [Hibernate]
        public string MediaFolder { get; set; }
        [Hibernate]
        public bool IsMediaFolderRecursive { get; set; }
        [Hibernate]
        public string AnimationFolder { get; set; }
        public ulong Id { get; set; }
        [Hibernate]
        public TServerType ServerType { get; set; }
        [Hibernate]
        public TMovieContainerFormat MovieContainerFormat { get; set; }
        [Hibernate]
        public List<CasparServerChannel> Channels { get; set; } = new List<CasparServerChannel>();
        [Hibernate]
        public List<CasparRecorder> Recorders { get; set; } = new List<CasparRecorder>();
        public override string ToString()
        {
            return ServerAddress;
        }

        public IDictionary<string, int> FieldLengths { get; }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Delete()
        {
            throw new NotImplementedException();
        }
    }

}
