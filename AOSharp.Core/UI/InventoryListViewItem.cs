using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.GameData;
using AOSharp.Core.Inventory;

namespace AOSharp.Core.UI
{
    public class InventoryListViewItem : MultiListViewItem
    {
        protected InventoryListViewItem(IntPtr pointer) : base(pointer)
        {
        }

        public virtual void Dispose()
        {
        }

        public static new InventoryListViewItem Create(int unk1, Identity dummyItem, bool unk2)
        {
            IntPtr pView = InventoryListViewItem_c.Create(unk1, dummyItem, unk2);

            if (pView == IntPtr.Zero)
                return null;

            return new InventoryListViewItem(pView);
        }

        public unsafe void SetColor(uint color) => ((MemStruct*)Pointer)->Color = color;

        public unsafe void SetIcon(int iconId)
        {
            ((MemStruct*)Pointer)->HasIcon = true;
            ((MemStruct*)Pointer)->IconId = iconId;
        }

        public unsafe bool GetDummyItem(out DummyItem dummyItem)
        {
            Identity dummyItemId = *(Identity*)(*(IntPtr*)(Pointer + 0x2C));

            return DummyItem.TryGet(dummyItemId, out dummyItem);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        protected unsafe struct MemStruct
        {
            [FieldOffset(0x68)]
            public uint Color;

            [MarshalAs(UnmanagedType.U1)]
            [FieldOffset(0x6C)]
            public bool HasIcon;

            [FieldOffset(0x70)]
            public int IconId;
        }
    }
}
