using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;
using System.Runtime.InteropServices;

namespace AOLite.Wrappers
{
    public class N3Message_t : UnmanagedClassBase
    {
        [DllImport("MessageProtocol.dll", EntryPoint = "??0N3Message_t@@QAE@IIPAVACE_Data_Block@@@Z", CallingConvention = CallingConvention.ThisCall)]
        public static extern IntPtr Constructor(IntPtr pThis, int unk, int serverId, IntPtr pAceDataBlock);

        [DllImport("MessageProtocol.dll", EntryPoint = "?DataBlockSizeGet@Message_t@@QBEIXZ", CallingConvention = CallingConvention.ThisCall)]
        public static extern int DataBlockSizeGet(IntPtr pThis);

        public N3Message_t(int serverId, IntPtr pAceDataBlock) : base(0x24)
        {
            Constructor(Pointer, 29, serverId, pAceDataBlock);
        }

        public int GetSize() => DataBlockSizeGet(Pointer);
    }
}
