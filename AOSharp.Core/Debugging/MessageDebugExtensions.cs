using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Core.Debugging
{
    public static class MessageDebugExtensions
    {
        public static Dictionary<string, string> Describe(this MessageBody msg)
        {
            if (msg is HealthDamageMessage healthDamageMsg)
                return DescribeHealthDamageMessage(healthDamageMsg);
            else if (msg is StatMessage statMsg)
                return DescribeStatMessage(statMsg);
            else if (msg is CharacterActionMessage charActionMsg)
                return DescribeCharacterActionMessage(charActionMsg);

            throw new NotImplementedException();
        }

        public static Dictionary<string, string> DescribeHealthDamageMessage(HealthDamageMessage msg)
        {
            Dictionary<string, string> descriptor = new Dictionary<string, string>();

            descriptor["Target"] = DynelManager.Find(msg.Identity, out Dynel source) ? source.Name : $"<DYNEL NOT FOUND: {msg.Identity}>";
            descriptor["Source"] = DynelManager.Find(msg.Target, out Dynel target) ? target.Name : $"<DYNEL NOT FOUND: {msg.Target}>";
            descriptor["Stat"] = msg.Stat.ToString();
            descriptor["Amount"] = msg.Amount.ToString();
            descriptor["TargetHP"] = msg.TargetHp.ToString();
            descriptor["Unk1"] = msg.Unk1.ToString();
            descriptor["Unk2"] = msg.Unk2.ToString();

            return descriptor;
        }

        public static Dictionary<string, string> DescribeStatMessage(StatMessage msg)
        {
            Dictionary<string, string> descriptor = new Dictionary<string, string>();

            descriptor["Affected"] = DynelManager.Find(msg.Identity, out Dynel affected) ? affected.Name : $"<DYNEL NOT FOUND: {msg.Identity}>";
            descriptor["Stat"] = string.Join(", ", msg.Stats.Select(x => $"{x.Value1}: {x.Value2}"));

            return descriptor;
        }

        public static Dictionary<string, string> DescribeCharacterActionMessage(CharacterActionMessage msg)
        {
            Dictionary<string, string> descriptor = new Dictionary<string, string>();

            descriptor["Affected"] = DynelManager.Find(msg.Identity, out Dynel affected) ? affected.Name : $"<DYNEL NOT FOUND: {msg.Identity}>";
            descriptor["Action"] = msg.Action.ToString();

            return descriptor;
        }
    }
}
