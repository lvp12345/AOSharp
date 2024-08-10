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
    public class CharacterAction
    {
        public static EventHandler<InspectEventArgs> Inspect;


        internal static void OnInspected(Identity target, InspectSlotInfo[] slotInfo)
        {
            Dictionary<IdentityType, List<InspectSlotInfo>> items = new Dictionary<IdentityType, List<InspectSlotInfo>>
            {
                { IdentityType.WeaponPage, new List<InspectSlotInfo>()},
                { IdentityType.ArmorPage, new List<InspectSlotInfo>()},
                { IdentityType.ImplantPage, new List<InspectSlotInfo>()},
                { IdentityType.SocialPage, new List<InspectSlotInfo>()},
            };

            foreach (var item in slotInfo)
            {
                IdentityType identityType = 
                    (int)item.EquipSlot <= (int)EquipSlot.Weap_Hud2 ? IdentityType.WeaponPage :
                    (int)item.EquipSlot <= (int)EquipSlot.Cloth_LeftFinger ? IdentityType.ArmorPage :
                    (int)item.EquipSlot <= (int)EquipSlot.Imp_Feet ? IdentityType.ImplantPage :
                    IdentityType.SocialPage;

                items[identityType].Add(item);
            }

            Inspect?.Invoke(null, new InspectEventArgs { Target = target, Pages = items });
        }

        public static void InfoRequest(Identity identity)
        {
            Network.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.InfoRequest,
                Identity = DynelManager.LocalPlayer.Identity,
                Target = identity,
            });
        }
    }

    public class InspectEventArgs
    {
        public Identity Target;
        public Dictionary<IdentityType, List<InspectSlotInfo>> Pages;
    }
}
