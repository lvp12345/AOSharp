using AOSharp.Common.GameData;
using SharpNav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Navigator.BT
{
    public class NavigatorContext
    {
        internal AONavigator Navigator;

        internal Queue<NavigatorTask> Tasks = new Queue<NavigatorTask>();

        internal Dictionary<PlayfieldId, NavMesh> NavmeshCache = new Dictionary<PlayfieldId, NavMesh>();

        public NavigatorContext(AONavigator navigator)
        {
            Navigator = navigator;
        }
    }
}
