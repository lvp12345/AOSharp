using EasyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOLite
{
    public static class Hooker
    {
        private static List<LocalHook> _hooks = new List<LocalHook>();

        public static void CreateHook(string module, string funcName, Delegate newFunc)
        {
            CreateHook(LocalHook.GetProcAddress(module, funcName), newFunc);
        }

        public static void CreateHook(IntPtr origFunc, Delegate newFunc)
        {
            LocalHook hook = LocalHook.Create(origFunc, newFunc, null);
            hook.ThreadACL.SetInclusiveACL(new Int32[] { 0 });
            _hooks.Add(hook);
        }

        public static void Unhook()
        {
            foreach(LocalHook hook in _hooks)
                hook.Dispose();
        }
    }
}
