using System;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;
using Serilog.Core;

namespace AOLite.Wrappers
{
    public class N3ClientEngine : UnmanagedClassBase
    {
        public N3ClientEngine() : base(0x130)
        {
            N3EngineClientAnarchy_t.Constructor(Pointer);
        }

        public void OpenClient(ResourceDatabase rdb, int clientInst)
        {
            N3EngineClientAnarchy_t.OpenClient(Pointer, rdb.Pointer, clientInst);
        }

        public void RunEngine(float deltaTime)
        {
            N3EngineClientAnarchy_t.RunEngine(Pointer, deltaTime);
        }

        public void Close()
        {
            N3Engine_t.Close(Pointer);
        }
    }
}
