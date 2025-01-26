using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using System.Runtime.InteropServices;
using AOLite.Debugging;

namespace AOLite.Wrappers
{
    public class N3InterfaceModule : UnmanagedClassBase
    {
        public N3InterfaceModule() : base(N3InterfaceModule_t.GetInstance())
        {
        }

        public int GetCharID() => N3InterfaceModule_t.GetCharID();

        public void SetCharID(int id) => N3InterfaceModule_t.SetCharID(id);

        public void ProcessMessage(byte[] dataBlock)
        {
            IntPtr pMessage = MessageProtocol.DataBlockToMessage((uint)dataBlock.Length, dataBlock);
            N3InterfaceModule_t.ProcessMessage(Pointer, pMessage);
        }

        public N3ClientEngine CreateN3Engine(ResourceDatabase rdb)
        {
            N3ClientEngine engine = new N3ClientEngine();
            engine.OpenClient(rdb, GetCharID());

            // Set n3EngineClientAnarchy_t * N3InterfaceModule_t::m_pcN3
            IntPtr pcN3 = Kernel32.GetProcAddress(Kernel32.GetModuleHandle("Interfaces.dll"), "?m_pcN3@N3InterfaceModule_t@@0PAVn3EngineClientAnarchy_t@@A");
            Marshal.WriteIntPtr(pcN3, engine.Pointer);

            return engine;
        }

        public void Shutdown()
        {
            N3InterfaceModule_t.ShutdownMessage();
        }
    }
}
