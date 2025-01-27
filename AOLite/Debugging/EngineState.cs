using AOSharp.Common;
using AOSharp.Core;
using AOSharp.Core.Debugging;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AOLite.Debugging
{
    public class EngineState
    {
        public int ClientControlId;
        public Stack<TickBlock> TickBlocks;

        internal EngineState(int clientControlId)
        {
            ClientControlId = clientControlId;
            TickBlocks = new Stack<TickBlock>();
            TickBlocks.Push(new TickBlock());
        }

        private EngineState(int clientControlId, Stack<TickBlock> tickBlocks)
        {
            ClientControlId = clientControlId;
            TickBlocks = tickBlocks;
        }

        internal void AddDataBlock(byte[] dataBlock)
        {
            TickBlock currentBlock = TickBlocks.Peek();

            if (currentBlock.Ticks.Any())
            {
                currentBlock = new TickBlock();
                TickBlocks.Push(currentBlock);
            }

            currentBlock.DataBlocks.Add(dataBlock);
        }

        internal void AddTick(float deltaTime)
        {
            TickBlocks.Peek().Ticks.Add(deltaTime);
        }

        public static EngineState LoadState(string path)
        {
            int clientControlId;
            Stack<TickBlock> tickBlocks = new Stack<TickBlock>();

            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    clientControlId = reader.ReadInt32();
                    int numBlocks = reader.ReadInt32();

                    for(int i = 0; i < numBlocks; i++)
                    {
                        TickBlock tickBlock = new TickBlock();

                        int numDataBlocks = reader.ReadInt32();

                        for (int j = 0; j < numDataBlocks; j++)
                            tickBlock.DataBlocks.Add(reader.ReadBytes(reader.ReadInt16()));

                        int numTicks = reader.ReadInt32();

                        for (int j = 0; j < numTicks; j++)
                            tickBlock.Ticks.Add(reader.ReadSingle());

                        tickBlocks.Push(tickBlock);
                    }
                }
            }

            return new EngineState(clientControlId, tickBlocks);
        }

        public void SaveState(string path)
        {
            using(FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(ClientControlId);

                    writer.Write(TickBlocks.Count);

                    while (TickBlocks.Any())
                    {
                        TickBlock block = TickBlocks.Pop();

                        writer.Write(block.DataBlocks.Count);

                        foreach (var datablock in block.DataBlocks)
                        {
                            writer.Write((short)datablock.Length);
                            writer.Write(datablock);
                        }

                        writer.Write(block.Ticks.Count);

                        foreach (var tick in block.Ticks)
                            writer.Write(tick);
                    }
                }
            }
        }

        public List<string> DescribeBlock(TickBlock block)
        {
            return block.DataBlocks.Select(x => DescribeDataBlock(x)).ToList();
        }

        public string DescribeDataBlock(byte[] dataBlock)
        {
            MessageSerializer _serializer = new MessageSerializer();
            Message message = _serializer.Deserialize(dataBlock);
            N3Message n3Msg = (N3Message)message.Body;

            string description = $"{n3Msg.N3MessageType}\nRAW:\n\t{BitConverter.ToString(dataBlock).Replace("-", "")}\n";
            string desribed = "DESCRIBED: \n";

            try
            {
                var descriptor = n3Msg.Describe();

                foreach (var keyPair in descriptor)
                    desribed += $"\t{keyPair.Key}: {keyPair.Value}\n";
            }
            catch (NotImplementedException)
            {
                desribed += "\tNOT IMPLEMENTED";
            }

            return description + desribed;
        }

        public class TickBlock
        {
            public List<byte[]> DataBlocks = new List<byte[]>();
            public List<float> Ticks = new List<float>();
        }
    }
}
