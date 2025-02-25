﻿using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin("moxo.pixel.menuoverhaul", "MoxoPixel-MenuOverhaul", "1.0.6")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            Settings.Init(Config);

            new MenuOverhaulPatch().Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();
            new OnGameStartedPatch().Enable();
            new OnGameEndedPatch().Enable();

            LogSource.LogInfo("MenuOverhaul by MoxoPixel loaded");
        }
    }
}