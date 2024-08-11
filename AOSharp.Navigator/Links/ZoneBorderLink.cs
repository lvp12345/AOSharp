using AOSharp.Common.GameData;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AOSharp.Navigator
{
    public class ZoneBorderLink : PlayfieldLink
    {
        [JsonConverter(typeof(Vector3ListConverter))]
        public List<Vector3> TransitionSpots;

        public ZoneBorderLink(PlayfieldId dstId, List<Vector3> transitionSpots) : base(dstId)
        {
            TransitionSpots = transitionSpots;
        }
    }
}
