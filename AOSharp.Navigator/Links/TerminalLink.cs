using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOSharp.Navigator
{
    public class TerminalLink : PlayfieldLink
    {
        public string TerminalName;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 TerminalPos;

        public TerminalLink(PlayfieldId dstId, string terminalName, Vector3 terminalPos) : base(dstId)
        {
            TerminalName = terminalName;
            TerminalPos = terminalPos;
        }

        public override string ToString()
        {
            return $"{GetType()} - Dst: {DstId}, Terminal Name: {TerminalName}, Terminal Pos: {TerminalPos}";
        }
    }
}
