using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOSharp.Navigator
{
    public class TeleporterLink : PlayfieldLink
    {
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 TeleporterPos;

        public TeleporterLink(PlayfieldId dstId, Vector3 teleporterPos) : base(dstId)
        {
            TeleporterPos = teleporterPos;
        }
    }
}
