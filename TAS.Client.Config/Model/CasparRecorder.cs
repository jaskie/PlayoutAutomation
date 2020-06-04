using TAS.Database.Common;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.Model
{
    public class CasparRecorder : IConfigRecorder
    {
        public object Owner { get; set; }
        
        [Hibernate]
        public int Id { get; set; }
        
        [Hibernate]
        public string RecorderName { get; set; }
        
        [Hibernate]
        public int DefaultChannel { get; set; }

        public override string ToString()
        {
            return $"{RecorderName} ({Id})";
        }
    }
}
