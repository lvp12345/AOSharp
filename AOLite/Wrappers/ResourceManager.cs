using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using System.Runtime.InteropServices;

namespace AOLite.Wrappers
{
    public class ResourceManager : UnmanagedClassBase
    {
        [DllImport("ResourceManager.dll", EntryPoint = "?Get@ResourceManager@@SAAAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Get();

        [DllImport("ResourceManager.dll", EntryPoint = "?SetDatabase@ResourceManager@@QAEXPAVDatabaseController_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void SetDatabase(IntPtr pThis, IntPtr pRdb);

        public ResourceManager() : base(Get())
        {
        }

        public void SetDatabase(ResourceDatabase rdb) => SetDatabase(Pointer, rdb.Pointer);
    }
}
