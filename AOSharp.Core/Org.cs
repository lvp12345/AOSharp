using System;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Core
{
    public static class Org
    {
        public static Action<OrganizationInfo> InfoReceived;

        public static void Info(Dynel dynel)
        {
            Info(dynel.Identity);
        }

        public static void Info(Identity target)
        {
            Network.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.Info,
                Target = target,
                Unknown = 0,
                Unknown1 = 4
            });
        }

        public static void Invite(SimpleChar target)
        {
            Invite(target.Identity);
        }

        public static void Invite(Identity target)
        {
            Network.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.Invite,
                Target = target,
                Unknown = 0,
                Unknown1 = 4
            });
        }

        public static void Kick(SimpleChar target)
        {
            Kick(target.Identity, target.Name);
        }
    
        public static void Kick(Identity identity, string name)
        {
            Network.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.Kick,
                Target = identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = name
                }
            });
        }

        public static void BankAdd(int amount)
        {
            Network.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.BankAdd,
                Target = DynelManager.LocalPlayer.Identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = amount.ToString()
                }
            });
        }

        public static void BankRemove(int amount)
        {
            Network.Send(new OrgClientMessage
            {
                Command = OrgClientCommand.BankRemove,
                Target = DynelManager.LocalPlayer.Identity,
                Unknown = 0,
                Unknown1 = 4,
                IOrgClientMessage = new OrgClientCommandArgsMessage
                {
                    CommandArgs = amount.ToString()
                }
            });
        }
    }
}
