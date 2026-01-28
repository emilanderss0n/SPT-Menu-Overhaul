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
        private static bool isAlignmentCameraMoved;
        private static readonly Dictionary<string, string> ButtonNameToFileNameMap = new Dictionary<string, string>
        {
            { "PlayButton", "icon_play" },
            { "CharacterButton", "icon_mainmenu_character" },
            { "TradeButton", "icon_trade" },
            { "HideoutButton", "hideout_icon_black" },
            { "ExitButton", "exit_status_runner" },
            { "ExitButtonGroup", "exit_status_runner" }
        };
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public class EnvironmentObjects
        {
            public GameObject EnvironmentUI { get; set; }
            public GameObject CommonObj { get; set; }
            public GameObject EnvironmentUISceneFactory { get; set; }
            public GameObject FactoryLayout { get; set; }
        }

        private static AssetBundle GetIconAssetBundle()
        {
            if (iconAssetBundle == null)
            {
                string assetBundlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MoxoPixel.MenuOverhaul", "Resources", "menu_overhaul_ui.bundle");
                if (!File.Exists(assetBundlePath))
                {
                    Plugin.LogSource.LogError($"AssetBundle not found at path: {assetBundlePath}");
                    return null;
                }
                iconAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
                if (iconAssetBundle == null)
                {
                    Plugin.LogSource.LogError("Failed to load AssetBundle!");
                    return null;
                }
            }
            return iconAssetBundle;
        }


        public static EnvironmentObjects FindEnvironmentObjects()
        {
            GameObject environmentUI = GameObject.Find("Environment UI");
            if (environmentUI == null) { Plugin.LogSource.LogWarning("Environment UI GameObject not found."); return null; }

            GameObject commonObj = environmentUI.transform.Find("Common")?.gameObject;
            if (commonObj == null) { Plugin.LogSource.LogWarning("Common GameObject not found in Environment UI."); return null; }

            GameObject environmentUISceneFactory = environmentUI.transform.Find("EnvironmentUISceneFactory")?.gameObject;
            if (environmentUISceneFactory == null) { Plugin.LogSource.LogWarning("EnvironmentUISceneFactory GameObject not found in Environment UI."); return null; }

            GameObject factoryLayout = environmentUISceneFactory.transform.Find("FactoryLayout")?.gameObject;
            if (factoryLayout == null) { Plugin.LogSource.LogWarning("FactoryLayout GameObject not found in EnvironmentUISceneFactory."); return null; }

            return new EnvironmentObjects
            {
                EnvironmentUI = environmentUI,
                CommonObj = commonObj,
                EnvironmentUISceneFactory = environmentUISceneFactory,
                FactoryLayout = factoryLayout
            };
        }

        public static GameObject GetGlowCanvas()
        {
            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects?.CommonObj == null)
            {
                Plugin.LogSource.LogWarning("GetGlowCanvas - EnvironmentObjects or CommonObj not found.");
                return null;
            }
            GameObject glowCanvas = envObjects.CommonObj.transform.Find("Glow Canvas")?.gameObject;
            if (glowCanvas == null) Plugin.LogSource.LogWarning("Glow Canvas GameObject not found in CommonObj.");
            return glowCanvas;
        }

        public static GameObject GetBackgroundPlane()
        {
            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects?.FactoryLayout == null)
            {
                Plugin.LogSource.LogWarning("GetBackgroundPlane - EnvironmentObjects or FactoryLayout not found.");
                return null;
            }
            GameObject backgroundPlane = envObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (backgroundPlane == null) Plugin.LogSource.LogWarning("CustomPlane GameObject not found in FactoryLayout.");
            return backgroundPlane;
        }

        public static void HideGameObject(MenuScreen instance, string fieldName)
        {
            if (instance == null || string.IsNullOrEmpty(fieldName)) return;
            var field = typeof(MenuScreen).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var gameObject = field.GetValue(instance) as GameObject;
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"Field '{fieldName}' not found in MenuScreen for HideGameObject.");
            }
        }


        public static void SetChildActive(GameObject parent, string childName, bool isActive)
        {
            if (parent == null || string.IsNullOrEmpty(childName)) return;
            
            Transform childTransform = parent.transform.Find(childName);
            if (childTransform != null)
            {
                childTransform.gameObject.SetActive(isActive);
                if (childName == "LampContainer" && isActive)
                {
                    ConfigureLampContainer(childTransform);
                }
            }
            else
            {
                Plugin.LogSource.LogDebug($"{childName} not found in {parent.name}.");
            }
        }


        public static void SetIconImages(GameObject buttonObject, string buttonName)
        {
            if (buttonObject == null)
            {
                Plugin.LogSource.LogWarning($"{buttonName} buttonObject is null for SetIconImages.");
                return;
            }

            AssetBundle bundle = GetIconAssetBundle();
            if (bundle == null) return;

            if (!ButtonNameToFileNameMap.TryGetValue(buttonName, out string fileName))
            {
                Plugin.LogSource.LogWarning($"No icon mapping found for button name: {buttonName}");
                return;
            }

            Sprite newIconSprite = bundle.LoadAsset<Sprite>(fileName);
            if (newIconSprite == null)
            {
                Plugin.LogSource.LogWarning($"Icon sprite '{fileName}' for {buttonName} could not be loaded from AssetBundle.");
                return;
            }

            SetImageComponentSprite(buttonObject, newIconSprite);
            Transform iconTransform = buttonObject.transform.Find("SizeLabel/IconContainer/Icon");
            if (iconTransform != null)
            {
                SetImageComponentSprite(iconTransform.gameObject, newIconSprite);
            }
        }

        private static void SetImageComponentSprite(GameObject obj, Sprite sprite)
        {
            if (obj == null) return;
            Image image = obj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.overrideSprite = sprite;
            }
        }

        public static bool IsPartOfMenuScreen(DefaultUIButtonAnimation buttonAnimation)
        {
            if (buttonAnimation == null) return false;
            Transform currentTransform = buttonAnimation.transform;
            while (currentTransform != null)
            {
                if (currentTransform.name == "MenuScreen") return true;
                currentTransform = currentTransform.parent;
            }
            return false;
        }

        public static bool IsMatchMaker()
        {
            GameObject matchmakerScreen = GameObject.Find("Menu UI/UI/Matchmaker Time Has Come");
            return matchmakerScreen != null && matchmakerScreen.activeInHierarchy;
        }


        private static Texture2D LoadAndPreparePanoramaTexture(AssetBundle bundle)
        {
            if (bundle == null) return null;

            float aspectRatio = (float)Screen.width / Screen.height;
            string textureName = aspectRatio > 2.33f ? "background_ultrawide" : "background";

            if (TextureCache.TryGetValue(textureName, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = bundle.LoadAsset<Texture2D>(textureName);
            if (texture == null)
            {
                Plugin.LogSource.LogWarning($"Texture '{textureName}' could not be loaded from AssetBundle.");
                return null;
            }

            texture = FlipTextureVertically(texture);
            texture = FlipTextureHorizontally(texture);
            TextureCache[textureName] = texture;
            return texture;
        }

        private static List<Material> ApplyEmissionToPanoramaMaterials(Renderer panoramaRenderer, Texture2D emissionTexture)
        {
            if (panoramaRenderer == null || emissionTexture == null) return new List<Material>();

            string[] materialNames = ["part1", "part2", "part3", "part4"];
            List<Material> appliedMaterials = new List<Material>();

            foreach (string materialName in materialNames)
            {
                Material material = panoramaRenderer.materials.FirstOrDefault(mat => mat.name.Contains(materialName));
                if (material != null)
                {
                    material.SetTexture(EmissionMap, emissionTexture);
                    material.EnableKeyword("_EMISSION");
                    appliedMaterials.Add(material);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"Material with name containing '{materialName}' not found on panorama renderer.");
                }
            }
            return appliedMaterials;
        }

        private static void CreateCustomPlaneForPanorama(GameObject factoryLayout, GameObject panoramaSource, List<Material> materialsToApply)
        {
            if (factoryLayout == null || panoramaSource == null || materialsToApply == null || !materialsToApply.Any()) 
            {
                Plugin.LogSource.LogError($"CreateCustomPlaneForPanorama - Invalid input parameters: factoryLayout={factoryLayout!=null}, panoramaSource={panoramaSource!=null}, materialsToApply={materialsToApply?.Count ?? 0}");
                return;
            }

            GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            newPlane.name = "CustomPlane";
            newPlane.transform.SetParent(factoryLayout.transform);
            newPlane.transform.localPosition = new Vector3(0f, 0f, 5.399f);
            newPlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            newPlane.transform.localScale = new Vector3(Settings.ScaleBackgroundX.Value, 1f, Settings.ScaleBackgroundY.Value);

            newPlane.layer = panoramaSource.layer;
            newPlane.tag = panoramaSource.tag;

            Renderer newPlaneRenderer = newPlane.GetComponent<Renderer>();
            if (newPlaneRenderer != null)
            {
                newPlaneRenderer.materials = materialsToApply.ToArray();
                newPlaneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                newPlaneRenderer.receiveShadows = false;
                newPlaneRenderer.allowOcclusionWhenDynamic = false;
            }
            else
            {
                Plugin.LogSource.LogError("Failed to get Renderer on newly created CustomPlane.");
            }
        }

        public static void SetPanoramaEmissionMap(GameObject factoryLayout, bool forceReload = false)
        {
            if (factoryLayout == null)
            {
                Plugin.LogSource.LogWarning("SetPanoramaEmissionMap - factoryLayout is null.");
                return;
            }

            if (forceReload)
            {
                ClearTextureCache();
            }

            AssetBundle bundle = GetIconAssetBundle();
            if (bundle == null)
            {
                Plugin.LogSource.LogError("Failed to get asset bundle for panorama emission map.");
                return;
            }

            GameObject panorama = factoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama == null)
            {
                Plugin.LogSource.LogWarning("panorama GameObject not found in FactoryLayout.");
                return;
            }

            bool wasActive = panorama.activeSelf;
            panorama.SetActive(true);

            Renderer panoramaRenderer = panorama.GetComponent<Renderer>();
            if (panoramaRenderer == null)
            {
                Plugin.LogSource.LogWarning("Renderer component not found on panorama.");
                panorama.SetActive(wasActive);
                return;
            }

            Texture2D preparedTexture = LoadAndPreparePanoramaTexture(bundle);
            if (preparedTexture == null)
            {
                Plugin.LogSource.LogWarning("Failed to prepare panorama texture.");
                panorama.SetActive(wasActive);
                return;
            }

            List<Material> appliedMaterials = ApplyEmissionToPanoramaMaterials(panoramaRenderer, preparedTexture);
            if (!appliedMaterials.Any())
            {
                Plugin.LogSource.LogWarning("Failed to apply emission to any panorama materials. Will recreate materials.");
                appliedMaterials = CreateDefaultMaterialsWithEmission(preparedTexture);
            }

            GameObject existingCustomPlane = factoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (existingCustomPlane != null)
            {
                UnityEngine.Object.Destroy(existingCustomPlane);
            }

            if (Settings.EnableBackground.Value && appliedMaterials.Any())
            {
                CreateCustomPlaneForPanorama(factoryLayout, panorama, appliedMaterials);
            }
            else if (!Settings.EnableBackground.Value)
            {
                Plugin.LogSource.LogDebug("SetPanoramaEmissionMap - CustomPlane creation skipped because EnableBackground is disabled.");
            }
            else
            {
                Plugin.LogSource.LogError("SetPanoramaEmissionMap - Failed to create any materials for the custom plane!");
            }
            
            panorama.SetActive(false);
        }

        private static List<Material> CreateDefaultMaterialsWithEmission(Texture2D emissionTexture)
        {
            if (emissionTexture == null) return new List<Material>();
            
            List<Material> materials = new List<Material>();
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            
            if (defaultMaterial != null)
            {
                defaultMaterial.SetTexture(EmissionMap, emissionTexture);
                defaultMaterial.SetTexture(MainTex, emissionTexture); 
                defaultMaterial.EnableKeyword("_EMISSION");
                defaultMaterial.SetColor(EmissionColor, Color.white);
                materials.Add(defaultMaterial);
            }
            else
            {
                Plugin.LogSource.LogError("Failed to create default material - Standard shader not found");
            }
            
            return materials;
        }

        public static Texture2D FlipTextureVertically(Texture2D original)
        {
            if (original == null) return null;
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
            if (original == null) return null;
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

        private static void ConfigureLampContainer(Transform lampContainerTransform)
        {
            if (lampContainerTransform == null) return;

            Transform lampTransform = lampContainerTransform.Find("Lamp");
            if (lampTransform != null)
            {
                lampTransform.gameObject.SetActive(true);
                Transform nestedLampTransform = lampTransform.Find("Lamp");
                if (nestedLampTransform != null)
                {
                    nestedLampTransform.gameObject.SetActive(true);
                    Transform pointLightBulbTransform = nestedLampTransform.Find("Point light_bulb");
                    if (pointLightBulbTransform != null)
                    {
                        pointLightBulbTransform.gameObject.SetActive(true);
                        Light pointLight = pointLightBulbTransform.GetComponent<Light>();
                        if (pointLight != null) pointLight.color = Color.white;
                        else Plugin.LogSource.LogWarning("Light component not found on Point light_bulb.");
                        pointLightBulbTransform.localPosition = new Vector3(-2.9435f, 1.2058f, 0.024f);
                    }
                    else { Plugin.LogSource.LogWarning("Point light_bulb not found in nested Lamp."); }

                    SetChildActive(nestedLampTransform.gameObject, "bulb", false);
                    SetChildActive(nestedLampTransform.gameObject, "flare_lampmenu", false);
                }
                else { Plugin.LogSource.LogWarning("Nested Lamp GameObject not found within Lamp."); }
            }
            else { Plugin.LogSource.LogWarning("Lamp GameObject not found within LampContainer."); }
        }

        private static void DeactivateDefaultMainMenuCamera(GameObject environmentUISceneFactory)
        {
            if (environmentUISceneFactory == null) return;
            GameObject factoryCameraContainer = environmentUISceneFactory.transform.Find("FactoryCameraContainer")?.gameObject;
            if (factoryCameraContainer == null) { Plugin.LogSource.LogWarning("FactoryCameraContainer GameObject not found."); return; }
            GameObject mainMenuCamera = factoryCameraContainer.transform.Find("MainMenuCamera")?.gameObject;
            if (mainMenuCamera != null) mainMenuCamera.SetActive(false);
            else Plugin.LogSource.LogWarning("MainMenuCamera GameObject not found in FactoryCameraContainer.");
        }

        private static void SetupCustomAlignmentCamera(GameObject environmentUI, GameObject factoryLayout)
        {
            if (environmentUI == null || factoryLayout == null) return;

            if (!isAlignmentCameraMoved)
            {
                Transform alignmentCameraOldPosTransform = environmentUI.transform.Find("AlignmentCamera");
                if (alignmentCameraOldPosTransform != null)
                {
                    if (alignmentCameraOldPosTransform.parent != factoryLayout.transform)
                    {
                        alignmentCameraOldPosTransform.SetParent(factoryLayout.transform);
                        isAlignmentCameraMoved = true;
                    }
                }
            }

            Transform alignmentCameraTransform = factoryLayout.transform.Find("AlignmentCamera");
            GameObject alignmentCamera;

            if (alignmentCameraTransform == null)
            {
                Plugin.LogSource.LogDebug("AlignmentCamera not found in FactoryLayout, creating new one.");
                alignmentCamera = new GameObject("AlignmentCamera");
                alignmentCamera.transform.SetParent(factoryLayout.transform);
                alignmentCamera.transform.localPosition = Vector3.zero;
                alignmentCamera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                alignmentCamera = alignmentCameraTransform.gameObject;
            }

            alignmentCamera.SetActive(true);
            Camera cameraComponent = alignmentCamera.GetComponent<Camera>();
            if (cameraComponent == null) cameraComponent = alignmentCamera.AddComponent<Camera>();
            cameraComponent.fieldOfView = 80;

            PrismEffects prismEffects = alignmentCamera.GetComponent<PrismEffects>();
            if (prismEffects == null) prismEffects = alignmentCamera.AddComponent<PrismEffects>();

            prismEffects.useChromaticAberration = true;
            prismEffects.chromaticIntensity = 0.03f;
            prismEffects.chromaticDistanceOne = 0.1f;
            prismEffects.chromaticDistanceTwo = 0.3f;
        }

        public static void DisableCameraMovement()
        {
            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects?.EnvironmentUISceneFactory == null || envObjects.FactoryLayout == null || envObjects.EnvironmentUI == null)
            {
                Plugin.LogSource.LogWarning("DisableCameraMovement - Essential EnvironmentObjects not found.");
                return;
            }
            DeactivateDefaultMainMenuCamera(envObjects.EnvironmentUISceneFactory);
            SetupCustomAlignmentCamera(envObjects.EnvironmentUI, envObjects.FactoryLayout);
        }


        /// <summary>
        /// Clears the texture cache and unloads the asset bundle to free memory
        /// </summary>
        public static void ClearTextureCache()
        {
            foreach (var texture in TextureCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            TextureCache.Clear();
            
            if (iconAssetBundle != null)
            {
                iconAssetBundle.Unload(false);
                iconAssetBundle = null;
            }
        }

        /// <summary>
        /// Performs cleanup of game objects related to the menu overhaul
        /// </summary>
        public static void CleanupGameObjects()
        {           
            try {
                EnvironmentObjects envObjects = FindEnvironmentObjects();
                if (envObjects == null || envObjects.FactoryLayout == null)
                {
                    Plugin.LogSource.LogWarning("CleanupGameObjects - Could not find environment objects.");
                    return;
                }

                Utility.ConfigureDecalPlane(false);
                
                GameObject customPlane = envObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
                if (customPlane != null && customPlane.activeSelf)
                {
                    customPlane.SetActive(false);
                }
                
                GameObject panorama = envObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
                if (panorama != null && panorama.activeSelf)
                {
                    panorama.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error during GameObject cleanup: {ex}");
            }

            Plugin.LogSource.LogDebug("Menu overhaul GameObjects cleanup completed");
        }

        public static void DisposeResources()
        {
            ClearTextureCache();
            isAlignmentCameraMoved = false;
            Plugin.LogSource.LogDebug("LayoutHelpers resources disposed");
        }

    }
}