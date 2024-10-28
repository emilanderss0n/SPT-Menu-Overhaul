using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin("moxo.pixel.menuoverhaul", "MoxoPixel-MenuOverhaul", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            LogSource.LogInfo("MenuOverhaul by MoxoPixel loaded");

            new MenuOverhaulPatch().Enable();
            LogSource.LogInfo("MenuOverhaul - MenuOverhaul patch loaded");
            new SetAlphaPatch().Enable();
            LogSource.LogInfo("MenuOverhaul - SetAlpha patch loaded");
            new TweenButtonPatch().Enable();
            LogSource.LogInfo("MenuOverhaul - TweenButton patch loaded");
        }
    }
}