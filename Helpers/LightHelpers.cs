using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        private static Transform mainLightTransform;
        private static Transform hairLightTransform;
        private static Transform fillLightTransform;
        private static Transform downLightTransform;
        private static Transform mainLightOneTransform;
        private static Transform mainLightTwoTransform;
        private static Transform mainLightThreeTransform;
        private static Transform mainLightFourTransform;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            if (mainLightTransform == null)
            {
                mainLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light");
            }
            if (mainLightTransform != null)
            {
                Light mainLight = mainLightTransform.GetComponent<Light>();
                if (mainLight != null)
                {
                    mainLight.color = new Color(0.5f, 0.7f, 1f, 1f);
                    mainLight.range = 10f;
                    mainLight.intensity = 0.26f;
                    if (Settings.EnableExtraShadows.Value)
                    {
                        mainLight.shadows = LightShadows.Soft;
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Main Light GameObject not found.");
            }

            if (hairLightTransform == null)
            {
                hairLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Hair Light");
            }
            if (hairLightTransform != null)
            {
                Light hairLight = hairLightTransform.GetComponent<Light>();
                if (hairLight != null)
                {
                    hairLight.intensity = 0.22f;
                    if (Settings.EnableExtraShadows.Value)
                    {
                        hairLight.shadows = LightShadows.Soft;
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Hair Light GameObject not found.");
            }

            if (fillLightTransform == null)
            {
                fillLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Fill Light");
            }
            if (fillLightTransform != null)
            {
                Light fillLight = fillLightTransform.GetComponent<Light>();
                if (fillLight != null)
                {
                    fillLight.intensity = 0.3f;
                    fillLight.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Fill Light GameObject not found.");
            }

            if (downLightTransform == null)
            {
                downLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Down Light");
            }
            if (downLightTransform != null)
            {
                downLightTransform.gameObject.SetActive(false);
            }
            else
            {
                Plugin.LogSource.LogWarning("Down Light GameObject not found.");
            }

            if (mainLightOneTransform == null)
            {
                mainLightOneTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light (1)");
            }
            if (mainLightOneTransform != null)
            {
                mainLightOneTransform.gameObject.SetActive(false);
            }
            else
            {
                Plugin.LogSource.LogWarning("Main Light (1) GameObject not found.");
            }

            if (mainLightTwoTransform == null)
            {
                mainLightTwoTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light (2)");
            }
            if (mainLightTwoTransform != null)
            {
                Light mainLightTwo = mainLightTwoTransform.GetComponent<Light>();
                if (mainLightTwo != null)
                {
                    mainLightTwo.intensity = 0.3f;
                    mainLightTwo.range = 10f;
                    mainLightTwo.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                mainLightTwoTransform.localPosition = new Vector3(0.95f, 0.5f, 4f);
            }
            else
            {
                Plugin.LogSource.LogWarning("Main Light (2) GameObject not found.");
            }

            if (mainLightThreeTransform == null)
            {
                mainLightThreeTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light (3)");
            }
            if (mainLightThreeTransform != null)
            {
                Light mainLightThree = mainLightThreeTransform.GetComponent<Light>();
                if (mainLightThree != null)
                {
                    mainLightThree.intensity = 0.25f;
                    mainLightThree.range = 2f;
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Main Light (3) GameObject not found.");
            }

            if (mainLightFourTransform == null)
            {
                mainLightFourTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light (4)");
            }
            if (mainLightFourTransform != null)
            {
                Light mainLightFour = mainLightFourTransform.GetComponent<Light>();
                if (mainLightFour != null)
                {
                    mainLightFour.intensity = 0.15f;
                    mainLightFour.range = 10f;
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Main Light (4) GameObject not found.");
            }
        }
    }
}