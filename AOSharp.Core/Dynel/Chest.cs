using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Core.Inventory;

namespace AOSharp.Core
{
    public class Chest : LockableContainer
    {
        public Chest(IntPtr pointer) : base(pointer)
        {
        }

        public Chest(Dynel dynel) : base(dynel)
        {
        }
    }
}
