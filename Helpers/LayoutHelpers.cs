using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    public static class LayoutHelpers
    {
        private static AssetBundle iconAssetBundle;
        private static bool isAlignmentCameraMoved = false;
        private static EnvironmentObjects cachedEnvironmentObjects;
        private static readonly Dictionary<string, string> ButtonNameToFileNameMap = new Dictionary<string, string>
        {
            { "PlayButton", "icon_play" },
            { "CharacterButton", "icon_mainmenu_character" },
            { "TradeButton", "icon_trade" },
            { "HideoutButton", "hideout_icon_black" },
            { "ExitButton", "exit_status_runner" },
            { "ExitButtonGroup", "exit_status_runner" }
        };
        private static readonly Dictionary<string, GameObject> gameObjectCache = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        public class EnvironmentObjects
        {
            public GameObject EnvironmentUI { get; set; }
            public GameObject CommonObj { get; set; }
            public GameObject EnvironmentUISceneFactory { get; set; }
            public GameObject FactoryLayout { get; set; }
        }

        public static GameObject GetGlowCanvas()
        {
            if (gameObjectCache.TryGetValue("GlowCanvas", out GameObject cachedGlowCanvas))
            {
                return cachedGlowCanvas;
            }

            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects == null)
            {
                Plugin.LogSource.LogWarning("EnvironmentObjects not found.");
                return null;
            }

            GameObject glowCanvas = envObjects.CommonObj.transform.Find("Glow Canvas")?.gameObject;
            if (glowCanvas == null)
            {
                Plugin.LogSource.LogWarning("Glow Canvas GameObject not found.");
            }
            else
            {
                gameObjectCache["GlowCanvas"] = glowCanvas;
            }

            return glowCanvas;
        }

        public static GameObject GetBackgroundPlane()
        {
            if (gameObjectCache.TryGetValue("BackgroundPlane", out GameObject cachedBackgroundPlane))
            {
                return cachedBackgroundPlane;
            }

            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects == null)
            {
                Plugin.LogSource.LogWarning("EnvironmentObjects not found.");
                return null;
            }

            GameObject backgroundPlane = envObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (backgroundPlane == null)
            {
                Plugin.LogSource.LogWarning("CustomPlane GameObject not found.");
            }
            else
            {
                gameObjectCache["BackgroundPlane"] = backgroundPlane;
            }

            return backgroundPlane;
        }

        public static void SetIconImages(GameObject buttonObject, string buttonName)
        {
            if (buttonObject == null)
            {
                Plugin.LogSource.LogWarning($"{buttonName} buttonObject is null.");
                return;
            }

            if (iconAssetBundle == null)
            {
                string assetBundlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MenuOverhaul", "Resources", "menu_overhaul_ui.bundle");

                if (!File.Exists(assetBundlePath))
                {
                    Plugin.LogSource.LogError($"AssetBundle not found at path: {assetBundlePath}");
                    return;
                }

                iconAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
                if (iconAssetBundle == null)
                {
                    Plugin.LogSource.LogError("Failed to load AssetBundle!");
                    return;
                }
            }

            if (!ButtonNameToFileNameMap.TryGetValue(buttonName, out string fileName))
            {
                Plugin.LogSource.LogWarning($"No mapping found for button name: {buttonName}");
                return;
            }

            Sprite newIconSprite = iconAssetBundle.LoadAsset<Sprite>(fileName);
            if (newIconSprite == null)
            {
                Plugin.LogSource.LogWarning($"Icon sprite for {buttonName} could not be loaded from AssetBundle.");
                return;
            }

            SetImageSprite(buttonObject, newIconSprite, buttonName);
            SetImageSprite(buttonObject.transform.Find("SizeLabel/IconContainer/Icon")?.gameObject, newIconSprite, buttonName);
        }

        private static void SetImageSprite(GameObject obj, Sprite sprite, string buttonName)
        {
            if (obj == null)
            {
                return;
            }

            Image image = obj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.overrideSprite = sprite;
            }
        }

        public static void SetPanoramaEmissionMap(GameObject factoryLayout)
        {
            if (iconAssetBundle == null)
            {
                string assetBundlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MenuOverhaul", "Resources", "menu_overhaul_ui.bundle");

                if (!File.Exists(assetBundlePath))
                {
                    Plugin.LogSource.LogError($"AssetBundle not found at path: {assetBundlePath}");
                    return;
                }

                iconAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
                if (iconAssetBundle == null)
                {
                    Plugin.LogSource.LogError("Failed to load AssetBundle!");
                    return;
                }
            }

            GameObject panorama = factoryLayout.transform.Find("panorama")?.gameObject;
            GameObject customPlane = factoryLayout.transform.Find("CustomPlane")?.gameObject;

            if (panorama == null)
            {
                Plugin.LogSource.LogWarning("panorama GameObject not found in FactoryLayout.");
                return;
            }

            Renderer renderer = panorama.GetComponent<Renderer>();
            if (renderer == null)
            {
                Plugin.LogSource.LogWarning("Renderer component not found on panorama.");
                return;
            }

            string[] materialNames = { "part1", "part2", "part3", "part4" };
            Texture2D texture;

            // Determine the screen aspect ratio
            float aspectRatio = (float)Screen.width / Screen.height;

            // Check if the aspect ratio is ultra-wide
            string textureName = aspectRatio > 2.33f ? "background_ultrawide" : "background";

            if (!textureCache.TryGetValue(textureName, out texture))
            {
                texture = iconAssetBundle.LoadAsset<Texture2D>(textureName);
                if (texture == null)
                {
                    Plugin.LogSource.LogWarning($"Texture {textureName} could not be loaded from AssetBundle.");
                    return;
                }

                texture = FlipTextureVertically(texture);
                texture = FlipTextureHorizontally(texture);
                textureCache[textureName] = texture;
            }

            List<Material> materials = new List<Material>();

            foreach (string materialName in materialNames)
            {
                Material material = renderer.materials.FirstOrDefault(mat => mat.name.Contains(materialName));
                if (material != null)
                {
                    material.SetTexture("_EmissionMap", texture);
                    material.EnableKeyword("_EMISSION");
                    materials.Add(material);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"Material with name containing '{materialName}' not found on panorama.");
                }
            }

            // Hide the panorama GameObject
            panorama.SetActive(false);

            // Check if CustomPlane already exists
            if (customPlane == null)
            {
                // Create a new plane mesh inside FactoryLayout
                GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                newPlane.name = "CustomPlane";
                newPlane.transform.SetParent(factoryLayout.transform);

                // Set the transforms
                newPlane.transform.localPosition = new Vector3(0f, 0f, 5.399f);
                newPlane.transform.position = new Vector3(0f, -999.999f, 5.399f);
                newPlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                newPlane.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                newPlane.transform.localScale = new Vector3(Settings.scaleBackgroundX.Value, 1f, Settings.scaleBackgroundY.Value);

                // Transfer other transforms from panorama object
                newPlane.layer = panorama.layer;
                newPlane.tag = panorama.tag;

                // Apply the materials to the new plane
                Renderer newPlaneRenderer = newPlane.GetComponent<Renderer>();
                newPlaneRenderer.materials = materials.ToArray();

                // Turn off shadow casting
                newPlaneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public static Texture2D FlipTextureVertically(Texture2D original)
        {
            Texture2D flipped = new Texture2D(original.width, original.height);
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    flipped.SetPixel(x, original.height - y - 1, original.GetPixel(x, y));
                }
            }
            flipped.Apply();
            return flipped;
        }

        public static Texture2D FlipTextureHorizontally(Texture2D original)
        {
            int width = original.width;
            int height = original.height;
            Texture2D flipped = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flipped.SetPixel(width - x - 1, y, original.GetPixel(x, y));
                }
            }

            flipped.Apply();
            return flipped;
        }

        public static void HideGameObject(MenuScreen instance, string fieldName)
        {
            var field = typeof(MenuScreen).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var gameObject = field.GetValue(instance) as GameObject;
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public static void SetChildActive(GameObject parent, string childName, bool isActive)
        {
            Transform childTransform = parent.transform.Find(childName);
            if (childTransform != null)
            {
                childTransform.gameObject.SetActive(isActive);

                // Special handling for LampContainer
                if (childName == "LampContainer" && isActive)
                {
                    // Show the Lamp object within LampContainer
                    Transform lampTransform = childTransform.Find("Lamp");
                    if (lampTransform != null)
                    {
                        lampTransform.gameObject.SetActive(true);

                        // Show the nested Lamp object within the first Lamp
                        Transform nestedLampTransform = lampTransform.Find("Lamp");
                        if (nestedLampTransform != null)
                        {
                            nestedLampTransform.gameObject.SetActive(true);

                            Transform pointLightBulbTransform = nestedLampTransform.Find("Point light_bulb");
                            if (pointLightBulbTransform != null)
                            {
                                pointLightBulbTransform.gameObject.SetActive(true);

                                Light pointLight = pointLightBulbTransform.GetComponent<Light>();
                                if (pointLight != null)
                                {
                                    pointLight.color = new Color(1f, 1f, 1f, 1f);
                                }
                                else
                                {
                                    Plugin.LogSource.LogWarning("Light component not found on Point light_bulb.");
                                }

                                // Set the localPosition
                                pointLightBulbTransform.localPosition = new Vector3(-2.9435f, 1.2058f, 0.024f);
                            }

                            Transform bulbTransform = nestedLampTransform.Find("bulb");
                            if (bulbTransform != null)
                            {
                                bulbTransform.gameObject.SetActive(false);
                            }

                            Transform flareLampMenuTransform = nestedLampTransform.Find("flare_lampmenu");
                            if (flareLampMenuTransform != null)
                            {
                                flareLampMenuTransform.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Nested Lamp GameObject not found within Lamp.");
                        }
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("Lamp GameObject not found within LampContainer.");
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"{childName} not found in {parent.name}.");
            }
        }

        public static EnvironmentObjects FindEnvironmentObjects()
        {
            if (cachedEnvironmentObjects != null)
            {
                return cachedEnvironmentObjects;
            }

            GameObject environmentUI = GameObject.Find("Environment UI");
            if (environmentUI == null)
            {
                Plugin.LogSource.LogWarning("Environment UI GameObject not found.");
                return null;
            }

            GameObject commonObj = environmentUI.transform.Find("Common")?.gameObject;
            if (commonObj == null)
            {
                Plugin.LogSource.LogWarning("Common GameObject not found.");
                return null;
            }

            GameObject environmentUISceneFactory = environmentUI.transform.Find("EnvironmentUISceneFactory")?.gameObject;
            if (environmentUISceneFactory == null)
            {
                Plugin.LogSource.LogWarning("EnvironmentUISceneFactory GameObject not found.");
                return null;
            }

            GameObject factoryLayout = environmentUISceneFactory.transform.Find("FactoryLayout")?.gameObject;
            if (factoryLayout == null)
            {
                Plugin.LogSource.LogWarning("FactoryLayout GameObject not found.");
                return null;
            }

            cachedEnvironmentObjects = new EnvironmentObjects
            {
                EnvironmentUI = environmentUI,
                CommonObj = commonObj,
                EnvironmentUISceneFactory = environmentUISceneFactory,
                FactoryLayout = factoryLayout
            };

            return cachedEnvironmentObjects;
        }

        public static void DisableCameraMovement()
        {
            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects == null)
            {
                Plugin.LogSource.LogWarning("EnvironmentObjects not found.");
                return;
            }

            GameObject factoryCameraContainer = envObjects.EnvironmentUISceneFactory.transform.Find("FactoryCameraContainer")?.gameObject;
            if (factoryCameraContainer == null)
            {
                Plugin.LogSource.LogWarning("FactoryCameraContainer GameObject not found.");
                return;
            }

            GameObject mainMenuCamera = factoryCameraContainer.transform.Find("MainMenuCamera")?.gameObject;
            if (mainMenuCamera == null)
            {
                Plugin.LogSource.LogWarning("MainMenuCamera GameObject not found.");
                return;
            }

            mainMenuCamera.SetActive(false);

            GameObject factoryLayout = envObjects.FactoryLayout;
            if (factoryLayout == null)
            {
                Plugin.LogSource.LogInfo("FactoryLayout GameObject not found.");
                return;
            }

            if (!isAlignmentCameraMoved)
            {
                GameObject alignmentCameraPos = envObjects.EnvironmentUI.transform.Find("AlignmentCamera")?.gameObject;
                if (alignmentCameraPos == null)
                {
                    Plugin.LogSource.LogInfo("AlignmentCamera GameObject not found.");
                    return;
                }

                if (alignmentCameraPos.transform.parent != factoryLayout.transform)
                {
                    alignmentCameraPos.transform.SetParent(factoryLayout.transform);
                    isAlignmentCameraMoved = true;
                }
            }

            GameObject alignmentCamera = envObjects.EnvironmentUI.transform.Find("EnvironmentUISceneFactory/FactoryLayout/AlignmentCamera")?.gameObject;
            if (alignmentCamera == null)
            {
                alignmentCamera = new GameObject("AlignmentCamera");
                alignmentCamera.transform.SetParent(factoryLayout.transform);

                Camera cameraComponent = alignmentCamera.AddComponent<Camera>();
                cameraComponent.fieldOfView = 80;
            }
            else
            {
                alignmentCamera.SetActive(true);

                Camera cameraComponent = alignmentCamera.GetComponent<Camera>();
                if (cameraComponent != null)
                {
                    cameraComponent.fieldOfView = 80;

                    var prismEffects = alignmentCamera.GetComponent<PrismEffects>();
                    if (prismEffects == null)
                    {
                        prismEffects = alignmentCamera.AddComponent<PrismEffects>();
                    }

                    prismEffects.useChromaticAberration = true;
                    prismEffects.chromaticIntensity = 0.03f;
                    prismEffects.chromaticDistanceOne = 0.1f;
                    prismEffects.chromaticDistanceTwo = 0.3f;
                }
            }
        }
        public static bool IsPartOfMenuScreen(DefaultUIButtonAnimation buttonAnimation)
        {
            Transform currentTransform = buttonAnimation.transform;
            while (currentTransform != null)
            {
                if (currentTransform.name == "MenuScreen")
                {
                    return true;
                }
                currentTransform = currentTransform.parent;
            }
            return false;
        }
        public static bool IsMatchMaker()
        {
            GameObject matchmakerScreen = GameObject.Find("Menu UI/UI/Matchmaker Time Has Come");
            while (matchmakerScreen == null)
            {
                return true;

            }
            return false;
        }
    }
}