using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.UI;
using AOSharp.Common.Helpers;
using AOSharp.Core.Combat;
using AOSharp.Core.Inventory;
using System.Reflection;
using AOSharp.Core.IPC;
using SmokeLounge.AOtomation.Messaging.Exceptions;
using AOSharp.Core.Movement;
using System.Data.SqlClient;
using Serilog.Core;
using Serilog;
using AOSharp.Core.Logging;

namespace AOSharp.Core
{
    public static class Game
    {
        public static float MaxFramerate
        {
            get {
                IntPtr pMaximumFramerate = Kernel32.GetProcAddress(Kernel32.GetModuleHandle("AFCM.dll"), "?m_vMaximumFramerate@Timer_t@@2MA");
                return Marshal.PtrToStructure<float>(pMaximumFramerate);
            }
            set {
                IntPtr pMaximumFramerate = Kernel32.GetProcAddress(Kernel32.GetModuleHandle("AFCM.dll"), "?m_vMaximumFramerate@Timer_t@@2MA");
                Marshal.StructureToPtr(value, pMaximumFramerate, true);
            }
        }
        public static bool IsZoning { get; private set; }
        public static int ClientInst => N3InterfaceModule_t.GetClientInst();

        public static int ServerId => Client_t.GetServerID(Client_t.GetInstanceIfAny());

        public static bool IsNewEngine => Kernel32.GetModuleHandle("Cheetah.dll") != IntPtr.Zero;

        public static bool IsAOLite => AppDomain.CurrentDomain.GetData("AOLite") != null;

        public static EventHandler<float> OnEarlyUpdate;
        public static EventHandler<float> OnUpdate;
        public static EventHandler TeleportStarted;
        public static EventHandler TeleportEnded;
        public static EventHandler TeleportFailed;
        public static EventHandler<uint> PlayfieldInit;

        private static Dictionary<IntPtr, Delegate> _vtblCache = new Dictionary<IntPtr, Delegate>();

        private static unsafe void Init()
        {
            DummyItem_t.GetSpellList = Marshal.GetDelegateForFunctionPointer<DummyItem_t.GetSpellListDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 8B 41 24 8B 4D 08 8B 04 88 5D C2 04 00"));
            DummyItem_t.GetSpellDataUnk = Marshal.GetDelegateForFunctionPointer<DummyItem_t.GetSpellDataUnkDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC FF 75 08 E8 ? ? ? ? 8B 45 08 5D C2 04 00"));
            DummyItem_t.GetSpellData = Marshal.GetDelegateForFunctionPointer<DummyItem_t.GetSpellDataDelegate>(Utils.FindPattern("Gamecode.dll", "8B 01 85 C0 75 04 33 D2 EB 02 8B 10 8B 41 08 8B C8 56 8B 72 08 D1 E9 83 E0 01 3B F1 77 02 2B CE 8B 52 04 8B 0C 8A 8D 04 C1 5E C3"));
            DummyItem_t.GetStat = Marshal.GetDelegateForFunctionPointer<DummyItem_t.GetStatDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 C1 34 8B 01 5D FF 60 04"));
            N3EngineClientAnarchy_t.GetItemActionInfo = Marshal.GetDelegateForFunctionPointer<N3EngineClientAnarchy_t.GetItemActionInfoDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 8B 49 6C 8D 45 08 50 E8 ? ? ? ? 8B 00 5D C2 04 00"));
            N3EngineClientAnarchy_t.GetMissionList = Marshal.GetDelegateForFunctionPointer<N3EngineClientAnarchy_t.GetMissionListDelegate>(Utils.FindPattern("Gamecode.dll", "B8 ? ? ? ? E8 ? ? ? ? 51 56 8B F1 83 BE ? ? ? ? ? 75 25 6A 18 E8 ? ? ? ? 59 8B C8 89 4D F0 83 65 FC 00 85 C9 74 08 56 E8 ? ? ? ? EB 02"));
            WeaponHolder_t.GetWeapon = Marshal.GetDelegateForFunctionPointer<WeaponHolder_t.GetWeaponDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 8B 45 08 56 8B F1 85 C0 78 05"));
            WeaponHolder_t.IsDynelInWeaponRange = Marshal.GetDelegateForFunctionPointer<WeaponHolder_t.IsDynelInWeaponRangeDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 EC 18 33 C0 56 8B F1 39 45 08 75 07"));
            WeaponHolder_t.IsInRange = Marshal.GetDelegateForFunctionPointer<WeaponHolder_t.IsInRangeDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 EC 18 56 8B F1 8B 4E 08 E8 ? ? ? ? 8B 48 5C 89 4D E8 8B 40 60 89 45 EC 8D 45 E8 50 FF 15 ? ? ? ? 59 89 45 F8 85 C0 0F 84"));
            WeaponHolder_t.GetDummyWeapon = Marshal.GetDelegateForFunctionPointer<WeaponHolder_t.GetDummyWeaponDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 51 56 8D 45 08 50 8B F1 8D 45 FC 50 8D 4E 0C E8 ? ? ? ? 8B 45 FC 3B 46 10 5E 74 05"));
            GamecodeUnk.FollowTarget = Marshal.GetDelegateForFunctionPointer<GamecodeUnk.FollowTargetDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 EC 24 53 56 8B D9 8B 83 ? ? ? ? 57 8B 7D 08 3B C7 0F 84 ? ? ? ? 83 A3 ? ? ? ? ? 85 C0 74 32 E8 ? ? ? ? 8B B3 ? ? ? ? 83 C0 14 50 8D 4E 14 E8 ? ? ? ? 84 C0 75 17 8D 83 ? ? ? ? 50 8D 4E 04 FF 15 ? ? ? ? 83 A3 ? ? ? ? ? 89 BB ? ? ? ? 85 FF 74 66 8B CB E8 ? ? ? ? 8B B3 ? ? ? ? 83 C0 14 50 8D 4E 14 E8 ? ? ? ? 84 C0 75 10 8D 83 ? ? ? ? 50 8D 4E 04"));

            if (!IsNewEngine)
            {
                GamecodeUnk.AppendSystemText = Marshal.GetDelegateForFunctionPointer<GamecodeUnk.AppendSystemTextDelegate>(Utils.FindPattern("Gamecode.dll", "B8 ? ? ? ? E8 ? ? ? ? 83 EC 28 53 56 FF 15 ? ? ? ? FF 75 0C 33 DB 8D 4D CC 8B F0 C7 45 ? ? ? ? ? 89 5D DC 88 5D CC E8 ? ? ? ? 8D 86 ? ? ? ? 8B 08 89 5D FC 3B CB 74 75 89 45 EC A1 ? ? ? ? 89 4D E8 8B 08 89 4D F0 8D 4D E8 89 08 C6 45 FC 01 8B 4D E8 8B 41 14 89 45 E8 8B 41 0C 2B C3 74 32 48 74 25 48 74 14 48 75 2E FF 75 10 8B 01 8D 55 CC 52 FF 75 08 FF 50 04 EB 1D 8B 01 8D 55 CC 52 FF 75 08 FF 50 04 EB 0F FF 75 08"));
                GamecodeUnk.IsInLineOfSight = Marshal.GetDelegateForFunctionPointer<GamecodeUnk.IsInLineOfSightDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 EC 30 56 8B 75 08 57 8B F9 85 F6 75 07 32 C0 E9 ? ? ? ? 3B F7 75 07 B0 01 E9 ? ? ? ? 8B 46 60 3B 47 60 75 E6 FF 15 ? ? ? ? 8B C8 89 4D 08 85 C9 74 D7 80 BF ? ? ? ? ? 74 13 80 BE ? ? ? ? ? 74 0A FF 15 ? ? ? ? 84 C0 74 C6 D9 EE 8B 4E 50 8B 35 ? ? ? ? D9 55 F4 D9 05 ? ? ? ? 53 D9 5D F8 D9 5D FC FF D6 D9 EE 8B 4F 50 D9 55 E8 8B D8 D9 05"));
            }
            else
            {
                GamecodeUnk.AppendSystemText = Marshal.GetDelegateForFunctionPointer<GamecodeUnk.AppendSystemTextDelegate>(Utils.FindPattern("Gamecode.dll", "B8 ? ? ? ? E8 ? ? ? ? 83 EC 28 53 56 FF 15 ? ? ? ? FF 75 0C 33 DB 8D 4D CC 8B F0 C7 45 ? ? ? ? ? 89 5D DC 88 5D CC E8 ? ? ? ? 8D 86 ? ? ? ? 8B 08 89 5D FC 3B CB 0F 84 ? ? ? ? 89 45 EC A1 ? ? ? ? 89 4D E8 8B 08 89 4D F0 8D 4D E8 89 08 C6 45 FC 01 8B 4D E8 8B F1 FF 15 ? ? ? ? 8B CE 89 45 E8 FF 15 ? ? ? ? 2B C3 74 38 48 74 29 48 74 16 48 75 36 FF 75 10 8B 06 8D 4D CC 51 FF 75 08 8B CE FF 50 04 EB 23 8B 06 8D 4D CC 51 FF 75 08 8B CE FF 50 04 EB 13"));
                GamecodeUnk.IsInLineOfSight = Marshal.GetDelegateForFunctionPointer<GamecodeUnk.IsInLineOfSightDelegate>(Utils.FindPattern("Gamecode.dll", "55 8B EC 83 EC 30 56 57 8B 7D 08 8B F1 85 FF 75 07 32 C0 E9 ? ? ? ? 3B FE 75 07 B0 01 E9 ? ? ? ? 53 FF 15 ? ? ? ? 8B CF 8B D8 FF 15 ? ? ? ? 3B C3 75 0E 8B CE FF 15 ? ? ? ? 8B D8 85 DB"));
            }

            if (!IsAOLite)
            {
                ChatWindowNode_t.AppendText = Marshal.GetDelegateForFunctionPointer<ChatWindowNode_t.AppendTextDelegate>(Utils.FindPattern("GUI.dll", "B8 ? ? ? ? E8 ? ? ? ? 83 EC 70 53 56 57 8B F1 68 ? ? ? ? 8D 4D D8 FF 15 ? ? ? ? 33 DB 89 5D FC 39 5D 0C 74 6B FF 75 0C 8D 45 84 50 E8 ? ? ? ? 50 8D 45 A0 68 ? ? ? ? 50 C6 45 FC 01 E8 ? ? ? ? 83 C4 14 68 ? ? ? ? 8D 4D BC 51 8B C8 C6 45 FC 02 FF 15 ? ? ? ? 50 8D 4D D8 C6 45 FC 03 FF 15 ? ? ? ? 8D 4D BC C6 45 FC 02 FF 15 ? ? ? ? 8D 4D A0 C6 45 FC 01 FF 15 ? ? ? ? 8D 4D 84 88 5D FC FF 15 ? ? ? ? FF 75 08 8D 45 84 50 E8 ? ? ? ? 59 59 50 8D 4D D8 C6 45 FC 04 FF 15 ? ? ? ? 53 6A 01 8D 4D 84 88 5D FC E8 ? ? ? ? 8B 3D ? ? ? ? 39 5D 0C 74 0A 68 ? ? ? ? 8D 4D D8"));
                GUIUnk.LoadViewFromXml = Marshal.GetDelegateForFunctionPointer<GUIUnk.LoadViewFromXmlDelegate>(Utils.FindPattern("GUI.dll", "55 8B EC 56 FF 75 10 FF 75 0C FF 15 ? ? ? ? 6A 00 68 ? ? ? ? 68 ? ? ? ? 8B F0 6A 00 56 E8 ? ? ? ? 8B 4D 08 83 C4 1C 89 01 85 C0 75 10 85 F6 74 08 8B 06 6A 01 8B CE FF 10 32 C0 EB 02 B0 01 5E 5D C3"));
                GUIUnk.UploadMissionToMap = Marshal.GetDelegateForFunctionPointer<GUIUnk.UploadMissionToMapDelegate>(Utils.FindPattern("GUI.dll", "B8 ? ? ? ? E8 ? ? ? ? 83 EC 2C D9 EE 56 8B 75 08 D9 55 C8 57 D9 55 CC 8D 45 D4 D9 55 D0 50 D9 55 D4 8D 45 C8 D9 55 D8 50 D9 5D DC 8D 45 EC 50 33 FF 56 89 7D EC 89 7D F0 FF 15 ? ? ? ? 8B C8 FF 15 ? ? ? ? 84 C0 0F 84 ? ? ? ? 53 8B 5D F0 FF 15 ? ? ? ? 05 ? ? ? ? 8B 08 3B CF 74 6C 89 45 E4 A1 ? ? ? ? 89 4D E0 8B 08 89 4D E8 8D 4D E0 89 08 89 7D FC 8B 4D E0 8B 41 14 89 45 E0 8B 41 0C 2B C7 74 2A 48 74 1F 48 74 10 48 75 26 8B 01 53 8D 55 D4 52 56 FF 50 04 EB 19"));

                //Inv UI testing
                ItemListViewBase_c.CreateItemListView = Marshal.GetDelegateForFunctionPointer<ItemListViewBase_c.CreateItemListViewDelegate>(Utils.FindPattern("GUI.dll", "B8 ? ? ? ? E8 ? ? ? ? 51 53 56 57 FF 75 14 8B F1 FF 75 10 89 75 F0 FF 75 0C FF 75 08 E8 ? ? ? ? 8B 3D ? ? ? ? 33 DB 8D 8E ? ? ? ? 89 5D FC C7 06 ? ? ? ? C7 46 ? ? ? ? ? C7 46 ? ? ? ? ? FF D7 8D 8E ? ? ? ? C6 45 FC 01 FF D7 8D 8E ? ? ? ? C6 45 FC 02 FF D7 8D 8E ? ? ? ? C6 45 FC 03 FF D7 8D 8E ? ? ? ? C6 45 FC 04 FF D7 8D 8E ? ? ? ? C6 45 FC 05 FF 15 ? ? ? ? C7 86"));
                InventoryListViewItem_c.CreateInventoryListViewItem = Marshal.GetDelegateForFunctionPointer<InventoryListViewItem_c.CreateInventoryListViewItemDelegate>(Utils.FindPattern("GUI.dll", "B8 ? ? ? ? E8 ? ? ? ? 83 EC 1C 53 56 57 FF 75 0C 8B F1 8D 4D D8 89 75 F0 FF 15 ? ? ? ? 8D 45 D8 33 DB 50 8B CE 89 5D FC E8 ? ? ? ? 8D 4D D8 C6 45 FC 02 FF 15 ? ? ? ? 8D 4E 48 C7 06 ? ? ? ? C7 46 ? ? ? ? ? FF 15 ? ? ? ? 8B 3D ? ? ? ? 8D 4E 4C C6 45 FC 03 FF D7 8D 8E ? ? ? ? C6 45 FC 04 FF D7 C7 86 ? ? ? ? ? ? ? ? 89 9E ? ? ? ? 88 9E ? ? ? ? 8D 8E ? ? ? ? 6A FF C6 45 FC 06 FF 15 ? ? ? ? D9 E8 8B 45 08 D9 9E ? ? ? ? 83 4E 68 FF 83 8E ? ? ? ? ? 89 86 ? ? ? ? 6A 11 33 C0 8D 7E 70 59 88 9E ? ? ? ? 89 9E ? ? ? ? C6 45 FC 07 F3 AB 38 5D 10"));

                int* pGetOptionWindowOffset = (int*)(Kernel32.GetProcAddress(Kernel32.GetModuleHandle("GUI.dll"), "?ModuleActivated@OptionPanelModule_c@@UAEX_N@Z") + 0x14);
                OptionPanelModule_c.GetOptionWindow = Marshal.GetDelegateForFunctionPointer<OptionPanelModule_c.GetOptionWindowDelegate>(new IntPtr((int)pGetOptionWindowOffset + sizeof(int) + *pGetOptionWindowOffset));
            }

            MovementController.Instance = new MovementController();
        }

        public unsafe static T GetVtbl<T>(IntPtr objPtr, int idx) where T : Delegate
        {
            IntPtr pVtbl = *((IntPtr*)objPtr);
            IntPtr vtblMethodAddr = *((IntPtr*)pVtbl + idx * 4);

            Delegate vtblMethod;
            if (_vtblCache.TryGetValue(vtblMethodAddr, out vtblMethod))
            {
                return (T)vtblMethod;
            }
            else
            {
                vtblMethod = Marshal.GetDelegateForFunctionPointer<T>(vtblMethodAddr);
                _vtblCache.Add(vtblMethodAddr, vtblMethod);

                return (T)vtblMethod;
            }
        }

        private static void Teardown()
        {
            UIController.Cleanup();

            if (MovementController.Instance != null && MovementController.Instance.IsNavigating)
                MovementController.Instance.Halt();
        }

        private static void OnEarlyUpdateInternal(float deltaTime)
        {
            if (DynelManager.LocalPlayer == null)
                return;

            OnEarlyUpdate?.Invoke(null, deltaTime);
        }

        private static void OnUpdateInternal(float deltaTime)
        {
            DynelManager.Update();

            if (DynelManager.LocalPlayer == null)
                return;

            Network.Update();

            Coroutine.Update();

            IPCChannel.UpdateInternal();

            UIController.UpdateViews();

            Item.Update();
            PerkAction.Update();
            Spell.Update();

            MovementController.Instance?.Update();
            CombatHandler.Instance?.Update(deltaTime);

            try
            {
                OnUpdate?.Invoke(null, deltaTime);
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }

            Chat.Update();
        }

        private static void OnTeleportStarted()
        {
            IsZoning = true;
            MovementController.Instance?.Halt();

            try
            {
                TeleportStarted?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        private static void OnTeleportEnded()
        {
            IsZoning = false;

            try
            {
                TeleportEnded?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        private static void OnTeleportFailed()
        {
            IsZoning = false;

            try
            {
                TeleportFailed?.Invoke(null, EventArgs.Empty);
            }
            catch { }
        }

        private static void OnPlayfieldInit(uint id)
        {
            PlayfieldInit?.Invoke(null, id);
        }
    }
}
