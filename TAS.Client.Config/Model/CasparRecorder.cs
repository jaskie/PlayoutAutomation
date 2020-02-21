using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparRecorder: IRecorderProperties
    {
        internal object Owner;
        public int Id { get; set; }
        public string RecorderName { get; set; }
        public int DefaultChannel { get; set; }
        public int ServerId { get; }
        public override string ToString()
        {
            return $"{RecorderName} ({Id})";
        }
    }
}
