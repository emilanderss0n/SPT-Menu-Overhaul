using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        private static Transform mainLightTransform;
        private static Transform hairLightTransform;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            mainLightTransform = SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light", SetupMainLight);
            hairLightTransform = SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Hair Light", SetupHairLight);
        }

        private static Transform SetupLight(GameObject parent, string name, Action<Light> setupAction)
        {
            Transform lightTransform = parent.transform.Find(name);
            if (lightTransform != null)
            {
                Light light = lightTransform.GetComponent<Light>();
                if (light != null)
                {
                    setupAction(light);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"Light component not found on {name}.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"{name} GameObject not found.");
            }
            return lightTransform;
        }

        private static void SetupMainLight(Light light)
        {
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void SetupHairLight(Light light)
        {
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        public static void UpdateLights()
        {
            UpdateLightShadows(mainLightTransform);
            UpdateLightShadows(hairLightTransform);
        }

        private static void UpdateLightShadows(Transform lightTransform)
        {
            if (lightTransform != null)
            {
                Light light = lightTransform.GetComponent<Light>();
                if (light != null)
                {
                    light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
                }
            }
        }
    }
}