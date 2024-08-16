using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System.Linq;
using AOSharp.Common.Unmanaged.Interfaces;
using AOSharp.Common.Unmanaged.Imports;
using SmokeLounge.AOtomation.Messaging.GameData;
namespace AOSharp.Core
{
    public unsafe class Mission
    {
        public static EventHandler<RollListChangedArgs> RollListChanged;

        public Identity Identity => (*(MissionMemStruct*)Pointer).Identity;

        public string DisplayName => GetDisplayName();

        public Identity Source => (*(MissionMemStruct*)Pointer).Source;

        public Identity Playfield => (*(MissionMemStruct*)Pointer).Playfield;

        public MissionLocation Location => N3EngineClientAnarchy.GetQuestWorldPos(Identity, out Identity pf, out Vector3 uniPos, out Vector3 pos) ? new MissionLocation(pf, uniPos, pos) : null;

        public List<MissionAction> Actions => GetActions();

        public static List<Mission> List => GetMissions();

        public readonly IntPtr Pointer;

        internal Mission(IntPtr pointer)
        {
            Pointer = pointer;
        }

        public static bool Find(Identity identity, out Mission mission)
        {
            return (mission = GetMissions().FirstOrDefault(x => x.Identity == identity)) != null;
        }

        public static bool Find(string displayName, out Mission mission)
        {
            return (mission = GetMissions().FirstOrDefault(x => x.DisplayName == displayName)) != null;
        }

        public static bool Exists(string displayName)
        {
            return GetMissions().Exists(x => x.DisplayName == displayName);
        }

        private static List<Mission> GetMissions()
        {
            LocalPlayer localPlayer = DynelManager.LocalPlayer;

            if (localPlayer == null)
                return new List<Mission>();

            return localPlayer.GetMissionList();
        }

        public void UploadToMap()
        {
            UploadToMap(Identity);
        }

        public void Delete()
        {
            Network.Send(new QuestMessage()
            {
                Action = QuestAction.Delete,
                Mission = Identity
            });
        }

        private string GetDisplayName()
        {
            return Utils.UnsafePointerToString(Pointer + 0x08);
        }

        private List<MissionAction> GetActions()
        {
            List<MissionAction> actions = new List<MissionAction>();

            foreach (IntPtr pAction in (*(MissionMemStruct*)Pointer).ActionList.ToList())
            {
                IntPtr pObjective = *(IntPtr*)(pAction + 0x8);

                if (pObjective == IntPtr.Zero)
                    continue;

                MissionActionMemStruct action = *(MissionActionMemStruct*)pObjective;

                switch (action.Type)
                {
                    case MissionActionType.FindPerson:
                        actions.Add(new FindPersonAction(action.Type, action.CharIdentity1));
                        break;
                    case MissionActionType.FindItem:
                        actions.Add(new FindItemAction(action.Type, action.ItemIdentity1));
                        break;
                    case MissionActionType.UseItemOnItem:
                        actions.Add(new UseItemOnItemAction(action.Type, action.ItemIdentity1, action.ItemIdentity2));
                        break;
                    case MissionActionType.KillPerson:
                        actions.Add(new KillPersonAction(action.Type, action.CharIdentity1));
                        break;
                }
            }

            return actions;
        }
        internal static void OnRollListChanged(MissionSliders missionSliders, MissionInfo[] missionInfo)
        {
            RollListChanged?.Invoke(null, new RollListChangedArgs(missionSliders, missionInfo));
        }

        public static void UploadToMap(Identity missionId)
        {
            GUIUnk.UploadMissionToMap(ref missionId);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 0x100)]
        private struct MissionMemStruct
        {
            [FieldOffset(0x00)]
            public Identity Identity;

            //0x08 - Size 36
            //DisplayName

            [FieldOffset(0x28)]
            public StdObjList ActionList;

            [FieldOffset(0x38)]
            public Identity Source;

            [FieldOffset(0xB4)]
            public Identity Playfield;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        private struct MissionActionMemStruct
        {
            [FieldOffset(0x00)]
            public MissionActionType Type;

            [FieldOffset(0x4)]
            public Identity ItemIdentity1;

            [FieldOffset(0xC)]
            public Identity ItemIdentity2;

            [FieldOffset(0x14)]
            public Identity CharIdentity1;

            [FieldOffset(0x1C)]
            public Identity CharIdentity2;

            [FieldOffset(0x20)]
            public Identity MobHash;
        }
    }

    public class MissionLocation
    {
        public readonly Identity Playfield;
        public readonly Vector3 UniversePos;
        public readonly Vector3 Pos;

        internal MissionLocation(Identity playfield, Vector3 uniPos, Vector3 pos)
        {
            Playfield = playfield;
            UniversePos = uniPos;
            Pos = pos;
        }
    }

    public class MissionAction
    {
        public MissionActionType Type;

        public MissionAction(MissionActionType type)
        {
            Type = type;
        }
    }

    public class FindPersonAction : MissionAction
    {
        public Identity Target;

        public FindPersonAction(MissionActionType type, Identity target) : base(type)
        {
            Target = target;
        }
    }

    public class FindItemAction : MissionAction
    {
        public Identity Target;

        public FindItemAction(MissionActionType type, Identity target) : base(type)
        {
            Target = target;
        }
    }

    public class UseItemOnItemAction : MissionAction
    {
        public Identity Source;
        public Identity Destination;

        public UseItemOnItemAction(MissionActionType type, Identity source, Identity destination) : base(type)
        {
            Source = source;
            Destination = destination;
        }
    }

    public class KillPersonAction : MissionAction
    {
        public Identity Target;
        public KillPersonAction(MissionActionType type, Identity target) : base(type)
        {
            Target = target;
        }
    }

    public class RollListChangedArgs : EventArgs
    {
        public MissionSliders MissionSliders { get; }
        public MissionInfo[] MissionDetails { get; }
        public Identity Identity { get; }

        public RollListChangedArgs(MissionSliders missionSliders, MissionInfo[] missionDetails)
        {
            MissionSliders = missionSliders;
            MissionDetails = missionDetails;
            Identity = Identity;
        }
    }
}
