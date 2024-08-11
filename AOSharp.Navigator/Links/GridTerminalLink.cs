using AOSharp.Common.GameData;

namespace AOSharp.Navigator
{
    public class GridTerminalLink : TerminalLink
    {
        public GridTerminalLink() : base(PlayfieldId.Grid, "Enter The Grid", Vector3.Zero)
        {
        }

        public GridTerminalLink(PlayfieldId dstId, Vector3 terminalPos) : base(dstId, "Enter The Grid", terminalPos)
        {
        }
    }
}
