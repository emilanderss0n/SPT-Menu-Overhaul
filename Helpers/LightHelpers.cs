using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;
using System.Collections.Generic;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        private static Transform mainLightTransform;
        private static Transform hairLightTransform;
        private static Transform mainLightThreeTransform;
        private static Dictionary<string, Transform> lightTransformsCache = new Dictionary<string, Transform>();

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            CacheLightTransforms(clonedPlayerModelView);
            mainLightThreeTransform = SetupLight("Main Light (3)", SetupMainLightThree);
            mainLightTransform = SetupLight("Main Light", SetupMainLight);
            hairLightTransform = SetupLight("Hair Light", SetupHairLight);
            SetupLight("Main Light (1)", SetupMainLightOne);
            SetupLight("Main Light (2)", SetupMainLightTwo);
            SetupLight("Main Light (4)", SetupMainLightFour);
            SetupLight("Fill Light", SetupFillLight);
            SetupLight("Down Light", SetupDownLight);
        }

        private static void CacheLightTransforms(GameObject parent)
        {
            lightTransformsCache.Clear();
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                lightTransformsCache[t.name] = t;
            }
        }

        private static Transform SetupLight(string name, Action<Light> setupAction)
        {
            if (lightTransformsCache.TryGetValue(name, out Transform lightTransform))
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