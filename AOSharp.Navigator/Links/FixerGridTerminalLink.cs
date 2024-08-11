using AOSharp.Common.GameData;

namespace AOSharp.Navigator
{
    public class FixerGridTerminalLink : UseOnTerminalLink
    {
        public FixerGridTerminalLink(Vector3 terminalPos) : base(PlayfieldId.FixerGrid, "Data Receptacle", "Enter The Grid", terminalPos)
        {
        }
    }
}
