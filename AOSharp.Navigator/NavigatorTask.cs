using AOSharp.Common.GameData;

namespace AOSharp.Navigator
{
    public class NavigatorTask
    {
        public readonly PlayfieldId DstId;

        protected NavigatorTask(PlayfieldId dstId)
        {
            DstId = dstId;
        }
    }
}
