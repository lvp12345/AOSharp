using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using System.Runtime.InteropServices;

namespace AOLite.Wrappers
{
    public class InstanceManager : UnmanagedClassBase
    {
        [DllImport("InstanceManager.dll", EntryPoint = "?Get@InstanceManager_t@@SAAAV1@XZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Get();

        [DllImport("InstanceManager.dll", EntryPoint = "?InitInfoObj@InstanceManager_t@@QAEXPAVInfoObject_t@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern void InitInfoObj(IntPtr pThis, IntPtr pInfoObj);

        public InstanceManager() : base(Get())
        {
        }

        public void InitInfoObj(IntPtr pInfoObj) => InitInfoObj(Pointer, pInfoObj);
    }
}
