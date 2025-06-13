using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;
using MoxoPixel.MenuOverhaul.Utils;
using System.Collections.Generic;
using System;
using SPT.Reflection.Patching;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin("moxo.pixel.menuoverhaul", "MoxoPixel-MenuOverhaul", "1.0.8")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource { get; private set; }

        private readonly List<ModulePatch> _patches = new List<ModulePatch>();

        private void Awake()
        {
            LogSource = Logger;
            Settings.Init(Config);

            InitializeAndEnablePatches();

            LogSource.LogInfo($"Plugin {Info.Metadata.Name} version {Info.Metadata.Version} loaded.");
        }

        private void InitializeAndEnablePatches()
        {
            _patches.Add(new MenuOverhaulPatch());
            _patches.Add(new PlayerProfileFeaturesPatch());
            _patches.Add(new SetAlphaPatch());
            _patches.Add(new TweenButtonPatch());
            _patches.Add(new OnGameStartedPatch());
            _patches.Add(new OnGameEndedPatch());

            foreach (var patch in _patches)
            {
                try
                {
                    patch.Enable();
                }
                catch (Exception ex)
                {
                    LogSource.LogError($"Failed to enable patch {patch.GetType().Name}: {ex}");
                }
            }
        }
    }
}