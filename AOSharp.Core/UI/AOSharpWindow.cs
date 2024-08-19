using AOSharp.Common.GameData;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using AOSharp.Core.Misc;
using AOSharp.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Core.UI
{
    public abstract class AOSharpWindow
    {
        public readonly string Name;
        public readonly string Path;
        public readonly WindowStyle Style;
        public readonly WindowFlags Flags;
        public Window Window;

        public AOSharpWindow(string name, string path, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade)
        {
            Name = name;
            Path = path;
            Style = windowStyle;
            Flags = flags;
        }

        public void Show()
        {
            if (Window != null && Window.IsValid)
                return;

            Window = Window.CreateFromXml(Name, Path, windowStyle: Style, windowFlags: Flags);

            OnWindowCreating();

            Window.Show(true);
        }

        public void Close()
        {
            if (Window == null || !Window.IsValid)
                return;

            Window.Close();
            Window = null;
        }

        protected abstract void OnWindowCreating();
    }
}
