using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin("moxo.pixel.menuoverhaul", "MoxoPixel-MenuOverhaul", "1.0.3")]
    [BepInDependency("com.fika.dedicated", BepInDependency.DependencyFlags.SoftDependency)] 
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            fikaDedicatedDetected = Chainloader.PluginInfos.Keys.Contains("com.fika.dedicated");
            if (fikaDedicatedDetected) {
                LogSource.LogInfo("MenuOverhaul by MoxoPixel not loaded, FIKA Dedicated plugin detected");
                return;
            }
            
            Settings.Init(Config);

            new MenuOverhaulPatch().Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();

            LogSource.LogInfo("MenuOverhaul by MoxoPixel loaded");
        }
    }
}
