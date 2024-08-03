using System;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.GameData;

namespace AOSharp.Core.UI
{
    public class MultiListViewItem
    {
        protected readonly IntPtr _pointer;
        public object Tag;
        public bool IsSelected => MultiListViewItem_c.IsSelected(_pointer);

        public IntPtr Pointer
        {
            get
            {
                return _pointer;
            }
        }

        protected MultiListViewItem(IntPtr pointer)
        {
            _pointer = pointer;
        }

        public static MultiListView GetListViewForPointer(IntPtr pointer)
        {
            IntPtr pListView = MultiListViewItem_c.GetListView(pointer);

            if (pListView == IntPtr.Zero)
                return null;

            if (UIController.FindViewByPointer(pListView, out MultiListView listView))
                return listView;

            return null;
        }

        public static MultiListViewItem Create(Variant variant)
        {
            IntPtr pView = MultiListViewItem_c.Create(variant);

            if (pView == IntPtr.Zero)
                return null;

            return new MultiListViewItem(pView);
        }

        public Variant GetID()
        {
            IntPtr pId = MultiListViewItem_c.GetID(_pointer);

            if (pId == IntPtr.Zero)
                return null;

            return Variant.FromPointer(pId, false);
        }

        public MultiListView GetListView()
        {
            return GetListViewForPointer(_pointer);
        }

        public void Select(bool selected, bool unk = false)
        {
            MultiListViewItem_c.Select(_pointer, selected, unk);
        }

        public void Invalidate()
        {
            MultiListViewItem_c.Invalidate(_pointer);
        }
    }
}
