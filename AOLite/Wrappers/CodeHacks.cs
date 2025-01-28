using AOSharp.Common.Unmanaged.Imports;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AOLite.Wrappers
{
    public class CodeHacks
    {
        public IntPtr _gamecodeBaseAddress;
        public IntPtr _n3BaseAddress;
        public IntPtr _randy31BaseAddress;
        public IntPtr _displaySystemBaseAddress;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate int DDamageVisualOutput(IntPtr ecx, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate byte DFloatingTextSpawner(IntPtr ecx, int a2, IntPtr a3, byte a4, int a5, int a6, int a7, int a8, int a9, int a10);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Unicode, SetLastError = true)]
        public delegate IntPtr DCharCastFSM(IntPtr ecx);

        public CodeHacks()
        {
            _gamecodeBaseAddress = Kernel32.GetModuleHandle("Gamecode.dll");
            _n3BaseAddress = Kernel32.GetModuleHandle("N3.dll"); 
            _randy31BaseAddress = Kernel32.GetModuleHandle("Randy31.dll");
            _displaySystemBaseAddress = Kernel32.GetModuleHandle("DisplaySystem.dll");
        }

        public void Install()
        {
            DisableBrokenResourceFrees();
            DisableHaltAnim();
            DisableVisualDynelVehicleAnim();
            DisableFlyingAnimStuff();
            DisableDynelEffectStuff();
            DisableCatmeshCreation();
            DisableDynelAnimCatMeshStuff();
            DisableRDBDynelVisualMeshCreation();
            DisableAnimUpdates();
            DisableSetMainDynel();
            DisablePlayfieldInit();
            DisableHealthDamageEffect();

            Hooker.CreateHook(_gamecodeBaseAddress + 0x7B1E3, new DCharCastFSM(CharCastFSM_Hook));
            Hooker.CreateHook(_gamecodeBaseAddress + 0x12C3E, new DDamageVisualOutput(DamageVisualOutput_Hook));
            Hooker.CreateHook(_gamecodeBaseAddress + 0x26D0, new DFloatingTextSpawner(FloatingTextSpawner_Hook));
        }

        private static IntPtr CharCastFSM_Hook(IntPtr ecx)
        {
            return ecx;
        }


        private static int DamageVisualOutput_Hook(IntPtr ecx, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9)
        {
            return 1;
        }

        private static byte FloatingTextSpawner_Hook(IntPtr ecx, int a2, IntPtr a3, byte a4, int a5, int a6, int a7, int a8, int a9, int a10)
        {
            return 1;
        }

        private unsafe void DisableHealthDamageEffect()
        {
            Patch(_gamecodeBaseAddress + 0xA0666, new byte[] { 0xE9, 0xF3, 0x00, 0x00, 0x00, 0x90 });
        }

        //This may or may not be slightly leaky but it only occurs on relog so it will be miniscule.
        private unsafe void DisableBrokenResourceFrees()
        {
            Patch(_gamecodeBaseAddress + 0x263E8, new byte[] { 0xEB });
            Patch(_randy31BaseAddress + 0x17F1B, new byte[] { 0xEB });
            Patch(_randy31BaseAddress + 0x4781F, new byte[] { 0xEB });
            Patch(_displaySystemBaseAddress + 0x36778, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
        }

        private unsafe void DisableHaltAnim()
        {
            Patch(_gamecodeBaseAddress + 0x3CA1E, new byte[] { 0xE9, 0x9F, 0x00, 0x00, 0x00 });
        }

        private unsafe void DisableVisualDynelVehicleAnim()
        {
            Patch(_gamecodeBaseAddress + 0x3CC4D, new byte[] { 0x90, 0x90 });
        }

        private unsafe void DisableFlyingAnimStuff()
        {
            Patch(_gamecodeBaseAddress + 0x6D9D7, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x6DA17, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x6DABE, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x6DAFE, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x6DBC9, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x6DCBF, new byte[] { 0xEB });
        }

        private unsafe void DisableDynelEffectStuff()
        {
            Patch(_gamecodeBaseAddress + 0x51C6C, new byte[] { 0xEB });
        }

        private unsafe void DisableCatmeshCreation()
        {
            Patch(_gamecodeBaseAddress + 0x5B932, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
            Patch(_gamecodeBaseAddress + 0x78604, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 });
            Patch(_gamecodeBaseAddress + 0x78611, new byte[] { 0xEB });
        }

        private unsafe void DisableRDBDynelVisualMeshCreation()
        {
            Patch(_gamecodeBaseAddress + 0x122462, new byte[] { 0xEB });
            Patch(_gamecodeBaseAddress + 0x1224EF, new byte[] { 0xEB });
        }

        private unsafe void DisableDynelAnimCatMeshStuff()
        {
            Patch(_gamecodeBaseAddress + 0x3C627, new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });
        }

        private unsafe void DisablePlayfieldInit()
        {
            Patch(_n3BaseAddress + 0x7680, new byte[] { 0x90, 0x90, 0x90, 0x90 });
        }

        private unsafe void DisableSetMainDynel()
        {
            Patch(_n3BaseAddress + 0x7AB7, new byte[] { 0x89, 0x86, 0x84, 0x00, 0x00, 0x00, 0xE9, 0x10, 0x01, 0x00, 0x00 });
        }

        private unsafe void DisableAnimUpdates()
        {
            Patch(_gamecodeBaseAddress + 0x6F020, new byte[] { 0xEB });
        }

        private unsafe void Patch(IntPtr address, byte[] replacementBytes)
        {
            Kernel32.VirtualProtect(address + replacementBytes.Length, (uint)sizeof(IntPtr), Kernel32.Protection.PAGE_READWRITE, out Kernel32.Protection oldProtection);
            
            for (int i = 0; i < replacementBytes.Length; i++)
                *(byte*)(address + i) = replacementBytes[i];

            Kernel32.VirtualProtect(address + replacementBytes.Length, (uint)sizeof(IntPtr), oldProtection, out _);
        }
    }
}
