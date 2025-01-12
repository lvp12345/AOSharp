using System;
using System.Runtime.InteropServices;
using System.Text;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports.DatabaseController;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;

namespace AOLite.Wrappers
{
    public class ResourceDatabase : UnmanagedClassBase
    {
        //Vtbl
        //2 - GetDbObject
        //4 - DeleteDbObject
        //8 - ErrNo

        public ResourceDatabase() : base(0x10)
        {
            ResourceDatabase_t.Constructor(Pointer);
        }

        public int Open(string path)
        {
            //StdString str = StdString.Create(path);

            byte[] bytes = Encoding.ASCII.GetBytes(path);
            IntPtr pStr = String_c.Constructor(Marshal.AllocHGlobal(0x100), bytes, bytes.Length);

            return ResourceDatabase_t.Open(Pointer, pStr, true);
        }

        public IntPtr GetInfoObject()
        {
            return GetDbObject(new DBIdentity(DBIdentityType.InfoObject, 1));
        }

        public IntPtr GetDbObject(DBIdentity identity)
        {
            return ResourceDatabase_t.GetDbObject(Pointer, ref identity);
        }

        public string GetLastErrorString()
        {
            IntPtr pStr = DatabaseController_t.ErrorStr(Pointer);
            return Marshal.PtrToStringAnsi(pStr);
        }
    }
}
