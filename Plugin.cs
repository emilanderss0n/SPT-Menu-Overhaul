using BepInEx.Logging;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoxoPixel.MenuOverhaul.Patches;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin("moxo.pixel.menuoverhaul", "MoxoPixel-MenuOverhaul", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            LogSource.LogInfo("MenuOverhaul by MoxoPixel loaded");

            new MenuOverhaulPatch().Enable();
        }
    }
}
