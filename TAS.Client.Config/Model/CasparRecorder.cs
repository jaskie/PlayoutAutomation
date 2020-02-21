using TAS.Common.Database;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparRecorder: IRecorderProperties
    {
        internal object Owner;
        
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
