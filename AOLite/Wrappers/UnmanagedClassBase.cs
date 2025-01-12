using AOSharp.Common.Unmanaged.Imports;
using System;
using System.Runtime.InteropServices;

namespace AOLite.Wrappers
{
    public unsafe class UnmanagedClassBase
    {
        protected delegate IntPtr Constructor(IntPtr pThis);
        public readonly IntPtr Pointer;
        private IntPtr Vtbl => *(IntPtr*)Pointer;

        protected UnmanagedClassBase(int size)
        {
            Pointer = Marshal.AllocHGlobal(size);
        }

        protected UnmanagedClassBase(IntPtr pointer)
        {
            Pointer = pointer;
        }

        protected virtual void Construct()
        {
        }

        public IntPtr GetVtblAddress(int index)
        {
            return *(IntPtr*)(Vtbl + IntPtr.Size * index);
        }

        protected void OverrideVtblFunc(int index, Delegate newFunc)
        {
            Kernel32.VirtualProtect(Vtbl + IntPtr.Size * index, (uint)sizeof(IntPtr), Kernel32.Protection.PAGE_READWRITE, out Kernel32.Protection oldProtection);
            *(IntPtr*)(Vtbl + IntPtr.Size * index) = Marshal.GetFunctionPointerForDelegate(newFunc);
        }
    }
}
