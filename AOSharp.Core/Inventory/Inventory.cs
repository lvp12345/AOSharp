using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Core.Inventory
{
    public static class Inventory
    {
        public static List<Item> Items => GetItems(DynelManager.LocalPlayer.Identity);

        public static readonly Bank Bank = new Bank();

        public static List<Backpack> Backpacks => Items.Where(x => x.UniqueIdentity.Type == IdentityType.Container).Select(x => new Backpack(x.UniqueIdentity, x.Slot)).ToList();

        public static bool IsFull => NumFreeSlots == 0;

        public static int NumFreeSlots => N3EngineClientAnarchy.GetNumberOfFreeInventorySlots();

        public static EventHandler<Container> ContainerOpened;

        public static bool Find(Identity identity, out Backpack backpack)
        {
            return (backpack = Backpacks.FirstOrDefault(x => x.Identity == identity)) != null;
        }

        public static bool Find(Identity slot, out Item item)
        {
            return (item = Items.FirstOrDefault(x => x.Slot == slot)) != null;
        }
        public static bool Find(string name, out Item item, bool includeEquipped = true)
        {
            return (item = FindAll(name, includeEquipped).FirstOrDefault()) != null;
        }

        public static bool Find(int id, out Item item, bool includeEquipped = true)
        {
            return (item = FindAll(id, includeEquipped).FirstOrDefault()) != null;
        }

        public static bool Find(int lowId, int highId, out Item item, bool includeEquipped = true)
        {
            return (item = FindAll(lowId, highId, includeEquipped).FirstOrDefault()) != null;
        }

        public static bool FindAtQl(int id, int ql, out Item item, bool includeEquipped = true)
        {
            return (item = FindAll(id, includeEquipped).FirstOrDefault(x => x.QualityLevel == ql)) != null;
        }

        public static List<Item> FindAll(int id, bool includeEquipped = true)
        {
            return Items.Where(x => (x.Id == id || x.HighId == id) && (includeEquipped || !x.IsEquipped)).ToList();
        }

        public static List<Item> FindAll(IEnumerable<int> ids)
        {
            return Items.Where(x => ids.Contains(x.Id) || ids.Contains(x.HighId)).ToList();
        }

        public static List<Item> FindAll(int lowId, int highId, bool includeEquipped = true)
        {
            return Items.Where(x => x.Id == lowId && x.HighId == highId && (includeEquipped || !x.IsEquipped)).ToList();
        }

        public static List<Item> FindAll(string name, bool includeEquipped = true)
        {
            return Items.Where(x => x.Name == name && (includeEquipped || !x.IsEquipped)).ToList();
        }

        public static bool FindContainer(Identity identity, out Backpack backpack)
        {
            return (backpack = Backpacks.FirstOrDefault(x => x.Identity == identity)) != null;
        }

        public static bool FindContainer(string name, out Backpack backpack)
        {
            return (backpack = Backpacks.FirstOrDefault(x => x.Name == name)) != null;
        }

        private static void OnContainerOpened(Identity identity)
        {
            ContainerOpened?.Invoke(null, new Container(identity));
        }

        //This will likely be made internal once I provide a way of accessing the inventory of all types of containers.
        //For now just utilize this if you REALLY need the contents of something random like contracts (and i guess bank too)
        public static unsafe List<Item> GetItems(Identity container)
        {
            List<Item> items = new List<Item>();
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return items;

            IntPtr pItems = N3EngineClientAnarchy_t.GetInventoryVec(pEngine, ref container);

            if (pItems == IntPtr.Zero)
                return items;

            int i = 0;
            foreach (IntPtr pItem in (*(StdStructVector*)pItems).ToList(sizeof(IntPtr)))
            {
                //Resolve proper type for item slot
                IdentityType slotType = IdentityType.None;

                switch (container.Type)
                {
                    case IdentityType.SimpleChar:
                        slotType = IdentityType.Inventory;

                        //Correct the slot type to match the equipment pages
                        if (i <= (int)EquipSlot.Weap_Hud2)
                            slotType = IdentityType.WeaponPage;
                        else if (i <= (int)EquipSlot.Cloth_LeftFinger)
                            slotType = IdentityType.ArmorPage;
                        else if (i <= (int)EquipSlot.Imp_Feet)
                            slotType = IdentityType.ImplantPage;
                        else if (i <= (int)EquipSlot.Social_LeftWeap)
                            slotType = IdentityType.SocialPage;

                        break;
                    case IdentityType.Bank:
                        slotType = IdentityType.BankByRef;
                        break;
                }

                IntPtr pActualItem = *(IntPtr*)pItem;

                if (pActualItem != IntPtr.Zero)
                {
                    try 
                    {
                        int charges = (*(ItemMemStruct*)pActualItem).Charges;
                        int lowId = (*(ItemMemStruct*)pActualItem).LowId;
                        int highId = (*(ItemMemStruct*)pActualItem).HighId;
                        int ql = (*(ItemMemStruct*)pActualItem).QualityLevel;
                        Identity unqiueIdentity = (*(ItemMemStruct*)pActualItem).UniqueIdentity;
                        items.Add(new Item(lowId, highId, ql, charges, unqiueIdentity, new Identity(slotType, i)));
                    } catch { }
                }

                i++;
            }

            return items;
        }

        public static unsafe List<Item> GetContainerItems(Identity identity)
        {
            List<Item> items = new List<Item>();

            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return items;

            IntPtr pInvList = N3EngineClientAnarchy_t.GetContainerInventoryList(pEngine, ref identity);

            if (pInvList == IntPtr.Zero)
                return items;

            IntPtr pItems = N3EngineClientAnarchy_t.GetInventoryVec(pEngine, ref identity);

            if (pItems == IntPtr.Zero)
                return items;

            List<IntPtr> containerInvList = (*(StdObjList*)pInvList).ToList();

            int i = 0;
            foreach (IntPtr pItem in (*(StdStructVector*)pItems).ToList(sizeof(IntPtr)))
            {
                IntPtr pActualItem = *(IntPtr*)pItem;

                if (pActualItem != IntPtr.Zero)
                {
                    try
                    {
                        int charges = (*(ItemMemStruct*)pActualItem).Charges;
                        int lowId = (*(ItemMemStruct*)pActualItem).LowId;
                        int highId = (*(ItemMemStruct*)pActualItem).HighId;
                        int ql = (*(ItemMemStruct*)pActualItem).QualityLevel;
                        Identity unqiueIdentity = (*(ItemMemStruct*)pActualItem).UniqueIdentity;
                        Identity slot = *((Identity*)(containerInvList[i] + 0x8));
                        items.Add(new Item(lowId, highId, ql, charges,unqiueIdentity, slot));
                    } catch {}
                    i++;
                }
            }

            return items;
        }

        internal static unsafe IntPtr GetBankInventoryEntry()
        {
            IntPtr inventoryHolderUnk = *(IntPtr*)(DynelManager.LocalPlayer.Pointer + 0x1B8);

            return *(IntPtr*)(inventoryHolderUnk + 0x17C);
        }

        internal static unsafe List<Item> GetBankItems()
        {
            List<Item> items = new List<Item>();

            IntPtr bankInventoryEntry = GetBankInventoryEntry();

            int i = 0;

            foreach (IntPtr pItem in (*(StdStructVector*)(bankInventoryEntry + 0xC)).ToList(sizeof(IntPtr)))
            {
                IntPtr pActualItem = *(IntPtr*)pItem;

                if (pActualItem != IntPtr.Zero)
                {
                    try
                    {
                        int charges = (*(ItemMemStruct*)pActualItem).Charges;
                        int lowId = (*(ItemMemStruct*)pActualItem).LowId;
                        int highId = (*(ItemMemStruct*)pActualItem).HighId;
                        int ql = (*(ItemMemStruct*)pActualItem).QualityLevel;
                        Identity unqiueIdentity = (*(ItemMemStruct*)pActualItem).UniqueIdentity;
                        Identity slot = new Identity(IdentityType.BankByRef, i);
                        items.Add(new Item(lowId, highId, ql, charges, unqiueIdentity, slot));
                    }
                    catch { }
                }

                i++;
            }

            return items;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        private struct ItemMemStruct
        {
            [FieldOffset(0x0)]
            public short Unk1; //Flags?

            [FieldOffset(0x2)]
            public ushort Charges;

            [FieldOffset(0x04)]
            public Identity UniqueIdentity;

            [FieldOffset(0x0C)]
            public int LowId;

            [FieldOffset(0x10)]
            public int HighId;

            [FieldOffset(0x14)]
            public int QualityLevel;

            [FieldOffset(0x18)]
            public int Quantity;

            [FieldOffset(0x24)]
            public int Unk2; //Some other flags?
        }
    }
}
