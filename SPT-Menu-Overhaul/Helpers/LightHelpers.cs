using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        private static Light mainLightComponent;
        private static Light hairLightComponent;
        private static Light mainLightAccentComponent;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("SetupLights - clonedPlayerModelView is null.");
                return;
            }
            mainLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light", ConfigureMainLight);
            mainLightAccentComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light (2)", ConfigureMainLightAccent);
            hairLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Hair Light", ConfigureHairLight);
        }

        private static Light FindAndSetupLight(GameObject parent, string path, Action<Light> configureAction)
        {
            if (parent == null || string.IsNullOrEmpty(path) || configureAction == null)
            {
                Plugin.LogSource.LogError("FindAndSetupLight - Invalid arguments.");
                return null;
            }

            Transform lightTransform = parent.transform.Find(path);
            if (lightTransform == null)
            {
                Plugin.LogSource.LogWarning($"Light GameObject not found at path: {path} in {parent.name}.");
                return null;
            }

            Light lightComponent = lightTransform.GetComponent<Light>();
            if (lightComponent == null)
            {
                Plugin.LogSource.LogWarning($"Light component not found on GameObject at path: {path} in {parent.name}.");
                return null;
            }

            configureAction(lightComponent);
            return lightComponent;
        }

        private static void ConfigureMainLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void ConfigureHairLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void ConfigureMainLightAccent(Light light)
        {
            if (light == null) return;
            light.color = Settings.AccentColor.Value;
        }

        public static void UpdateLights()
        {
            UpdateSingleLightShadows(mainLightComponent);
            UpdateSingleLightShadows(hairLightComponent);
            UpdateAccentLightColor();
            
            if (Utility.IsInGame())
            {
                Utility.DisableDecalPlaneIfInGame();
            }
        }

        public static void UpdateAccentLightColor()
        {
            if (mainLightAccentComponent != null)
            {
                mainLightAccentComponent.color = Settings.AccentColor.Value;
            }
        }

        private static void UpdateSingleLightShadows(Light lightComponent)
        {
            if (lightComponent != null)
            {
                lightComponent.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            }
        }

        public static void Cleanup()
        {
            mainLightComponent = null;
            hairLightComponent = null;
            mainLightAccentComponent = null;
            
            Plugin.LogSource.LogDebug("Light helper references cleared during cleanup");
        }
    }
}