using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TAS.Server.VideoSwitch.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommunicatorType
    {
        Nevion,
        BlackmagicSmartVideoHub,
        Atem,
        Ross,
        Unknown
    }
    public enum ListTypeEnum
    {
        Input,
        CrosspointChange,
        CrosspointStatus,
        SignalPresence
    }    
}
