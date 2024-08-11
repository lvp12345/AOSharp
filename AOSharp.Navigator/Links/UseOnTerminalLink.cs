using AOSharp.Common.GameData;

namespace AOSharp.Navigator
{
    public class UseOnTerminalLink : TerminalLink
    {
        public string ItemName;

        public UseOnTerminalLink(PlayfieldId dstId, string itemName, string terminalName, Vector3 terminalPos) : base(dstId, terminalName, terminalPos)
        {
            ItemName = itemName;
        }
    }
}
