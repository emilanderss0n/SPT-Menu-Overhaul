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
        private static readonly Dictionary<string, string> ButtonNameToFileNameMap = new Dictionary<string, string>
        {
            { "PlayButton", "icon_play" },
            { "CharacterButton", "icon_mainmenu_character" },
            { "TradeButton", "icon_trade" },
            { "HideoutButton", "hideout_icon_black" },
            { "ExitButton", "exit_status_runner" },
            { "ExitButtonGroup", "exit_status_runner" }
        };
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

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
                string assetBundlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MenuOverhaul", "Resources", "menu_overhaul_ui.bundle");
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

        private static Texture2D LoadAndPreparePanoramaTexture(AssetBundle bundle)
        {
            if (bundle == null) return null;

            float aspectRatio = (float)Screen.width / Screen.height;
            string textureName = aspectRatio > 2.33f ? "background_ultrawide" : "background";

            if (textureCache.TryGetValue(textureName, out Texture2D cachedTexture))
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
            textureCache[textureName] = texture;
            return texture;
        }

        private static List<Material> ApplyEmissionToPanoramaMaterials(Renderer panoramaRenderer, Texture2D emissionTexture)
        {
            if (panoramaRenderer == null || emissionTexture == null) return new List<Material>();

            string[] materialNames = { "part1", "part2", "part3", "part4" };
            List<Material> appliedMaterials = new List<Material>();

            foreach (string materialName in materialNames)
            {
                Material material = panoramaRenderer.materials.FirstOrDefault(mat => mat.name.Contains(materialName));
                if (material != null)
                {
                    material.SetTexture("_EmissionMap", emissionTexture);
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

            // Create a new plane primitive
            GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            newPlane.name = "CustomPlane";
            newPlane.transform.SetParent(factoryLayout.transform);

            // Position and rotate the plane
            newPlane.transform.localPosition = new Vector3(0f, 0f, 5.399f);
            newPlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            newPlane.transform.localScale = new Vector3(Settings.scaleBackgroundX.Value, 1f, Settings.scaleBackgroundY.Value);

            // Set layer and tag to match the source
            newPlane.layer = panoramaSource.layer;
            newPlane.tag = panoramaSource.tag;

            // Get and configure the renderer
            Renderer newPlaneRenderer = newPlane.GetComponent<Renderer>();
            if (newPlaneRenderer != null)
            {
                // Apply materials
                newPlaneRenderer.materials = materialsToApply.ToArray();
                newPlaneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                
                // Set additional renderer properties for better appearance
                newPlaneRenderer.receiveShadows = false;
                newPlaneRenderer.allowOcclusionWhenDynamic = false;
                
                // Log information about the materials
                for (int i = 0; i < materialsToApply.Count; i++)
                {
                    Material mat = materialsToApply[i];
                    if (mat != null)
                    {
                        bool hasEmission = mat.IsKeywordEnabled("_EMISSION");
                        Texture2D emissionMap = mat.GetTexture("_EmissionMap") as Texture2D;
                    }
                }
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

            // If forceReload is true, we clear the texture cache and unload the asset bundle to force a fresh load
            if (forceReload)
            {
                // Clear texture cache to force reloading textures
                foreach (var texture in textureCache.Values)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }
                textureCache.Clear();
                
                // Unload asset bundle to force reloading
                if (iconAssetBundle != null)
                {
                    iconAssetBundle.Unload(false); // false = don't unload assets, just the bundle reference
                    iconAssetBundle = null;
                }
            }

            // Make sure we have a fresh AssetBundle reference
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
                panorama.SetActive(wasActive); // Restore original state
                return;
            }

            Texture2D preparedTexture = LoadAndPreparePanoramaTexture(bundle);
            if (preparedTexture == null)
            {
                Plugin.LogSource.LogWarning("Failed to prepare panorama texture.");
                panorama.SetActive(wasActive); // Restore original state
                return;
            }

            List<Material> appliedMaterials = ApplyEmissionToPanoramaMaterials(panoramaRenderer, preparedTexture);
            if (!appliedMaterials.Any())
            {
                Plugin.LogSource.LogWarning("Failed to apply emission to any panorama materials. Will recreate materials.");
                // If we can't find proper materials, let's see if we can create new ones
                appliedMaterials = CreateDefaultMaterialsWithEmission(preparedTexture);
            }

            // Clean up any existing CustomPlane before creating a new one
            GameObject existingCustomPlane = factoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (existingCustomPlane != null)
            {
                UnityEngine.Object.Destroy(existingCustomPlane);
            }

            // Create the new custom plane
            if (appliedMaterials.Any())
            {
                CreateCustomPlaneForPanorama(factoryLayout, panorama, appliedMaterials);
            }
            else
            {
                Plugin.LogSource.LogError("SetPanoramaEmissionMap - Failed to create any materials for the custom plane!");
            }
            
            // Always hide the panorama mesh which we've replaced with our custom plane regardless of original state
            panorama.SetActive(false);
        }

        // Helper method to create default materials with emission if we can't find the original materials
        private static List<Material> CreateDefaultMaterialsWithEmission(Texture2D emissionTexture)
        {
            if (emissionTexture == null) return new List<Material>();
            
            List<Material> materials = new List<Material>();
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            
            if (defaultMaterial != null)
            {
                defaultMaterial.SetTexture("_EmissionMap", emissionTexture);
                defaultMaterial.SetTexture("_MainTex", emissionTexture); 
                defaultMaterial.EnableKeyword("_EMISSION");
                defaultMaterial.SetColor("_EmissionColor", Color.white);
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

                    LayoutHelpers.SetChildActive(nestedLampTransform.gameObject, "bulb", false);
                    LayoutHelpers.SetChildActive(nestedLampTransform.gameObject, "flare_lampmenu", false);
                }
                else { Plugin.LogSource.LogWarning("Nested Lamp GameObject not found within Lamp."); }
            }
            else { Plugin.LogSource.LogWarning("Lamp GameObject not found within LampContainer."); }
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

        // Method to clean up all GameObjects created by the mod when game starts
        public static void CleanupGameObjects()
        {           
            try {
                EnvironmentObjects envObjects = FindEnvironmentObjects();
                if (envObjects == null || envObjects.FactoryLayout == null)
                {
                    Plugin.LogSource.LogWarning("CleanupGameObjects - Could not find environment objects.");
                    return;
                }

                // Hide decal_plane and its child objects
                GameObject decalPlane = envObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
                if (decalPlane != null)
                {
                    // Disable main decal_plane
                    decalPlane.SetActive(false);
                    
                    // Also disable child objects - decal_plane_pve
                    Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                    if (pveTransform != null)
                    {
                        pveTransform.gameObject.SetActive(false);
                    }
                    
                    // Also disable child objects - decal_plane
                    Transform decalPlaneChildTransform = decalPlane.transform.Find("decal_plane");
                    if (decalPlaneChildTransform != null)
                    {
                        decalPlaneChildTransform.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("CleanupGameObjects - decal_plane GameObject not found");
                }

                // Hide CustomPlane if it exists
                GameObject customPlane = envObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
                if (customPlane != null)
                {
                    customPlane.SetActive(false);
                }
                
                // Keep panorama disabled
                GameObject panorama = envObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
                if (panorama != null)
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
    }
}