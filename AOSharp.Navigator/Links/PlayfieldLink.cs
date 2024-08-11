using AOSharp.Common.GameData;
using JsonSubTypes;
using Newtonsoft.Json;

namespace AOSharp.Navigator
{
    [JsonConverter(typeof(JsonSubtypes), "$type")]
    [JsonSubtypes.KnownSubType(typeof(TerminalLink), "TerminalLink")]
    [JsonSubtypes.KnownSubType(typeof(ZoneBorderLink), "ZoneBorderLink")]
    [JsonSubtypes.KnownSubType(typeof(TeleporterLink), "TeleporterLink")]
    [JsonSubtypes.KnownSubType(typeof(GridTerminalLink), "GridTerminalLink")]
    [JsonSubtypes.KnownSubType(typeof(UseOnTerminalLink), "UseOnTerminalLink")]
    public class PlayfieldLink : NavigatorTask
    {
        [JsonConstructor]
        protected PlayfieldLink(PlayfieldId dstId) : base(dstId)
        {
        }
    }
}
