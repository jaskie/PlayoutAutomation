using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TAS.Server.VideoSwitch.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommunicatorType
    {
        None,
        Nevion,
        BlackmagicSmartVideoHub,
        Atem,
        Ross
    }
    public enum ListTypeEnum
    {
        Input,
        CrosspointChange,
        CrosspointStatus,
        SignalPresence
    }    
}
