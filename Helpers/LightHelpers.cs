using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        private static Transform mainLightTransform;
        private static Transform hairLightTransform;
        private static Transform mainLightThreeTransform;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            mainLightThreeTransform = SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light (3)", SetupMainLightThree);
            mainLightTransform = SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light", SetupMainLight);
            hairLightTransform = SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Hair Light", SetupHairLight);
            SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light (1)", SetupMainLightOne);
            SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light (2)", SetupMainLightTwo);
            SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light (4)", SetupMainLightFour);
            SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Fill Light", SetupFillLight);
            SetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Down Light", SetupDownLight);
        }

        private static Transform SetupLight(GameObject parent, string path, Action<Light> setupAction)
        {
            Transform lightTransform = parent.transform.Find(path);
            if (lightTransform != null)
            {
                Light light = lightTransform.GetComponent<Light>();
                if (light != null)
                {
                    setupAction(light);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"Light component not found on {path}.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"{path} GameObject not found.");
            }
            return lightTransform;
        }

        private static void SetupMainLight(Light light)
        {
            light.color = new Color(0.5f, 0.7f, 1f, 1f);
            light.range = 10f;
            light.intensity = 0.26f;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void SetupHairLight(Light light)
        {
            light.intensity = 0.22f;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void SetupFillLight(Light light)
        {
            light.intensity = 0.3f;
            light.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static void SetupDownLight(Light light)
        {
            light.intensity = 0f;
        }

        private static void SetupMainLightOne(Light light)
        {
            light.intensity = 0f;
        }

        private static void SetupMainLightTwo(Light light)
        {
            light.intensity = 0.3f;
            light.range = 10f;
            light.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static void SetupMainLightThree(Light light)
        {
            light.intensity = 0.25f;
            light.range = 2f;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
        }

        private static void SetupMainLightFour(Light light)
        {
            light.intensity = 0.15f;
            light.range = 10f;
        }

        public static void UpdateLights()
        {
            UpdateLightShadows(mainLightTransform);
            UpdateLightShadows(hairLightTransform);
            UpdateLightShadows(mainLightThreeTransform);
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