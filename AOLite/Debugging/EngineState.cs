using AOSharp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOLite.Debugging
{
    internal class EngineState
    {
        internal List<byte[]> _processedDataBlocks = new List<byte[]>();
        
        internal void AddDataBlock(byte[] dataBlock)
        {
            _processedDataBlocks.Add(dataBlock);
        }

        internal void SaveState(string localPath)
        {
            var lastPacket = _processedDataBlocks.LastOrDefault();

            if (lastPacket == null)
                return;

            Console.WriteLine($"Last packet: {lastPacket.ToHexString()}");

            using(FileStream stream = new FileStream($"{localPath}\\AOLiteCrash_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.dat", FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(_processedDataBlocks.Count);

                    foreach(var datablock in _processedDataBlocks)
                    {
                        writer.Write((short)datablock.Length);
                        writer.Write(datablock);
                    }
                }
            }
        }
    }
}
