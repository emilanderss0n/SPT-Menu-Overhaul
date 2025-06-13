using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        // Store Light components directly instead of Transforms to avoid repeated GetComponent calls.
        private static Light mainLightComponent;
        private static Light hairLightComponent;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("SetupLights - clonedPlayerModelView is null.");
                return;
            }
            mainLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light", ConfigureMainLight);
            hairLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Hair Light", ConfigureHairLight);
        }

        // Renamed and refactored for clarity and direct Light component handling
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

        // Renamed for clarity
        private static void ConfigureMainLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            // Add any other specific configurations for the main light here
        }

        // Renamed for clarity
        private static void ConfigureHairLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            // Add any other specific configurations for the hair light here
        }

        public static void UpdateLights()
        {
            // Update shadows based on current settings
            UpdateSingleLightShadows(mainLightComponent);
            UpdateSingleLightShadows(hairLightComponent);
        }

        // Renamed for clarity
        private static void UpdateSingleLightShadows(Light lightComponent)
        {
            if (lightComponent != null)
            {
                lightComponent.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            }
            // else, the light was not found during setup, warning already logged.
        }
    }
}