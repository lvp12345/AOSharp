using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
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
                Unknown1 = 1
            });
        }
    }
}
