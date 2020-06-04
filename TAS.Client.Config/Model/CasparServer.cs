using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Database.Common;
using TAS.Common.Interfaces.Configurator;
using Newtonsoft.Json;

namespace TAS.Client.Config.Model
{
    public class CasparServer : IConfigCasparServer
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
        [JsonConverter(typeof(ConcreteListConverter<IConfigCasparChannel, CasparServerChannel>))]
        public List<IConfigCasparChannel> Channels { get; set; } = new List<IConfigCasparChannel>();
        
        [Hibernate]        
        [JsonConverter(typeof(ConcreteListConverter<IConfigRecorder, CasparRecorder>))]        
        public List<IConfigRecorder> Recorders { get; set; } = new List<IConfigRecorder>();
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
