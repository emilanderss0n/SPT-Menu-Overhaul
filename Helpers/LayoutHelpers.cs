using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    public static class LayoutHelpers
    {
        private static bool isAlignmentCameraMoved;
        private static readonly string PluginResourcesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MoxoPixel.MenuOverhaul", "Resources");
        private static readonly string IconsDirectory = Path.Combine(PluginResourcesRoot, "icons");
        private static readonly string BackgroundDirectory = Path.Combine(PluginResourcesRoot, "background");
        private static readonly Dictionary<string, string> ButtonNameToFileNameMap = new Dictionary<string, string>
        {
            { MenuOverhaulConstants.MenuButtons.PlayButton, "icon_play" },
            { MenuOverhaulConstants.MenuButtons.CharacterButton, "icon_mainmenu_character" },
            { MenuOverhaulConstants.MenuButtons.TradeButton, "icon_trade" },
            { MenuOverhaulConstants.MenuButtons.HideoutButton, "hideout_icon_black" },
            { MenuOverhaulConstants.MenuButtons.ExitButton, "exit_status_runner" },
            { MenuOverhaulConstants.MenuButtons.ExitButtonGroup, "exit_status_runner" }
        };
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
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

        public static EnvironmentObjects FindEnvironmentObjects()
        {
            GameObject environmentUI = GameObject.Find(MenuOverhaulConstants.Environment.EnvironmentUI);
            if (environmentUI == null) { Plugin.LogSource.LogWarning("Environment UI GameObject not found."); return null; }

            Transform commonTransform = environmentUI.transform.Find(MenuOverhaulConstants.Environment.Common);
            GameObject commonObj = commonTransform != null ? commonTransform.gameObject : null;
            if (commonObj == null) { Plugin.LogSource.LogWarning("Common GameObject not found in Environment UI."); return null; }

            Transform environmentUISceneFactoryTransform = environmentUI.transform.Find(MenuOverhaulConstants.Environment.EnvironmentUISceneFactory);
            GameObject environmentUISceneFactory = environmentUISceneFactoryTransform != null ? environmentUISceneFactoryTransform.gameObject : null;
            if (environmentUISceneFactory == null) { Plugin.LogSource.LogWarning("EnvironmentUISceneFactory GameObject not found in Environment UI."); return null; }

            Transform factoryLayoutTransform = environmentUISceneFactory.transform.Find(MenuOverhaulConstants.Environment.FactoryLayout);
            GameObject factoryLayout = factoryLayoutTransform != null ? factoryLayoutTransform.gameObject : null;
            if (factoryLayout == null) { Plugin.LogSource.LogWarning("FactoryLayout GameObject not found in EnvironmentUISceneFactory."); return null; }

            return new EnvironmentObjects
            {
                EnvironmentUI = environmentUI,
                CommonObj = commonObj,
                EnvironmentUISceneFactory = environmentUISceneFactory,
                FactoryLayout = factoryLayout
            };
        }

        public static GameObject GetBackgroundPlane()
        {
            EnvironmentObjects envObjects = FindEnvironmentObjects();
            if (envObjects == null || envObjects.FactoryLayout == null)
            {
                Plugin.LogSource.LogWarning("GetBackgroundPlane - EnvironmentObjects or FactoryLayout not found.");
                return null;
            }
            Transform backgroundPlaneTransform = envObjects.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
            GameObject backgroundPlane = backgroundPlaneTransform != null ? backgroundPlaneTransform.gameObject : null;
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

        public static void UpdateTopGlowColor(GameObject commonObj, Color accentColor)
        {
            if (commonObj == null)
            {
                return;
            }

            Transform topGlowTransform = commonObj.transform.Find(MenuOverhaulConstants.Environment.TopGlowPvePath);
            if (topGlowTransform == null)
            {
                return;
            }

            Graphic topGlowGraphic = topGlowTransform.GetComponent<Graphic>();
            if (topGlowGraphic == null)
            {
                return;
            }

            Color currentColor = topGlowGraphic.color;
            topGlowGraphic.color = new Color(accentColor.r, accentColor.g, accentColor.b, currentColor.a);
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

            if (!ButtonNameToFileNameMap.TryGetValue(buttonName, out string fileName))
            {
                Plugin.LogSource.LogWarning($"No icon mapping found for button name: {buttonName}");
                return;
            }

            Sprite newIconSprite = LoadIconSprite(fileName);
            if (newIconSprite == null)
            {
                Plugin.LogSource.LogWarning($"Icon sprite '{fileName}' for {buttonName} could not be loaded from '{IconsDirectory}'.");
                return;
            }

            SetImageComponentSprite(buttonObject, newIconSprite);
            Transform iconTransform = buttonObject.transform.Find(MenuOverhaulConstants.MenuButtons.IconPath);
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
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }
        }

        public static bool IsPartOfMenuScreen(DefaultUIButtonAnimation buttonAnimation)
        {
            if (buttonAnimation == null) return false;
            Transform currentTransform = buttonAnimation.transform;
            while (currentTransform != null)
            {
                if (currentTransform.name == MenuOverhaulConstants.MenuScreen.Name) return true;
                currentTransform = currentTransform.parent;
            }
            return false;
        }


        private static Texture2D LoadAndPreparePanoramaTexture()
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            string textureName = aspectRatio > 2.33f ? "background_ultrawide" : "background";

            if (TextureCache.TryGetValue(textureName, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = LoadTextureFromDirectory(BackgroundDirectory, textureName);
            if (texture == null)
            {
                Plugin.LogSource.LogWarning($"Texture '{textureName}' could not be loaded from '{BackgroundDirectory}'.");
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

            // IMPORTANT: read sharedMaterials and clone each one before mutating.
            // Accessing panoramaRenderer.materials would mutate the original
            // panorama renderer's instanced materials, causing our custom
            // background to bleed into every other menu screen (Hideout, Flea,
            // Matchmaker, in-raid ESC) that re-enables the original panorama.
            Material[] sourceMaterials = panoramaRenderer.sharedMaterials;

            foreach (string materialName in materialNames)
            {
                Material source = null;
                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material candidate = sourceMaterials[i];
                    if (candidate != null && candidate.name.Contains(materialName))
                    {
                        source = candidate;
                        break;
                    }
                }

                if (source != null)
                {
                    Material clone = new Material(source) { name = source.name + "_CustomPlane" };
                    clone.SetTexture(EmissionMap, emissionTexture);
                    clone.EnableKeyword("_EMISSION");
                    appliedMaterials.Add(clone);
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
            bool hasMaterials = materialsToApply != null && materialsToApply.Count > 0;
            if (factoryLayout == null || panoramaSource == null || !hasMaterials) 
            {
                int materialsCount = materialsToApply != null ? materialsToApply.Count : 0;
                Plugin.LogSource.LogError($"CreateCustomPlaneForPanorama - Invalid input parameters: factoryLayout={factoryLayout!=null}, panoramaSource={panoramaSource!=null}, materialsToApply={materialsCount}");
                return;
            }

            GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            newPlane.name = MenuOverhaulConstants.Environment.CustomPlane;
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

            Transform panoramaTransform = factoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
            GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
            if (panorama == null)
            {
                Plugin.LogSource.LogWarning("panorama GameObject not found in FactoryLayout.");
                return;
            }

            // We read sharedMaterials below, which does not require the renderer
            // to be active. Avoid toggling panorama.SetActive here so other screens
            // that re-enable the original panorama never see its custom-emission state.
            Renderer panoramaRenderer = panorama.GetComponent<Renderer>();
            if (panoramaRenderer == null)
            {
                Plugin.LogSource.LogWarning("Renderer component not found on panorama.");
                return;
            }

            Texture2D preparedTexture = LoadAndPreparePanoramaTexture();
            if (preparedTexture == null)
            {
                Plugin.LogSource.LogWarning("Failed to prepare panorama texture.");
                return;
            }

            List<Material> appliedMaterials = ApplyEmissionToPanoramaMaterials(panoramaRenderer, preparedTexture);
            if (appliedMaterials.Count == 0)
            {
                Plugin.LogSource.LogWarning("Failed to apply emission to any panorama materials. Will recreate materials.");
                appliedMaterials = CreateDefaultMaterialsWithEmission(preparedTexture);
            }

            Transform existingCustomPlaneTransform = factoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
            GameObject existingCustomPlane = existingCustomPlaneTransform != null ? existingCustomPlaneTransform.gameObject : null;
            if (existingCustomPlane != null)
            {
                UnityEngine.Object.Destroy(existingCustomPlane);
            }

            if (Settings.EnableBackground.Value && appliedMaterials.Count > 0)
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
                        if (pointLight != null)
                        {
                            pointLight.color = Settings.EnableLogotypeBulbAccentColor.Value
                                ? Settings.AccentColor.Value
                                : Color.white;
                        }
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
            Transform factoryCameraContainerTransform = environmentUISceneFactory.transform.Find(MenuOverhaulConstants.Environment.FactoryCameraContainer);
            GameObject factoryCameraContainer = factoryCameraContainerTransform != null ? factoryCameraContainerTransform.gameObject : null;
            if (factoryCameraContainer == null) { Plugin.LogSource.LogWarning("FactoryCameraContainer GameObject not found."); return; }
            Transform mainMenuCameraTransform = factoryCameraContainer.transform.Find(MenuOverhaulConstants.Environment.MainMenuCamera);
            GameObject mainMenuCamera = mainMenuCameraTransform != null ? mainMenuCameraTransform.gameObject : null;
            if (mainMenuCamera != null) mainMenuCamera.SetActive(false);
            else Plugin.LogSource.LogWarning("MainMenuCamera GameObject not found in FactoryCameraContainer.");
        }

        public static void UpdateLogotypeBulbLightColor(GameObject factoryLayout)
        {
            if (factoryLayout == null)
            {
                return;
            }

            Transform pointLightBulbTransform = factoryLayout.transform.Find(MenuOverhaulConstants.Environment.LogotypeBulbLightPath);
            if (pointLightBulbTransform == null)
            {
                return;
            }

            Light pointLight = pointLightBulbTransform.GetComponent<Light>();
            if (pointLight == null)
            {
                return;
            }

            pointLight.color = Settings.EnableLogotypeBulbAccentColor.Value
                ? Settings.AccentColor.Value
                : Color.white;
        }

        private static void SetupCustomAlignmentCamera(GameObject environmentUI, GameObject factoryLayout)
        {
            if (environmentUI == null || factoryLayout == null) return;

            if (!isAlignmentCameraMoved)
            {
                Transform alignmentCameraOldPosTransform = environmentUI.transform.Find(MenuOverhaulConstants.Environment.AlignmentCamera);
                if (alignmentCameraOldPosTransform != null)
                {
                    if (alignmentCameraOldPosTransform.parent != factoryLayout.transform)
                    {
                        alignmentCameraOldPosTransform.SetParent(factoryLayout.transform);
                        isAlignmentCameraMoved = true;
                    }
                }
            }

            Transform alignmentCameraTransform = factoryLayout.transform.Find(MenuOverhaulConstants.Environment.AlignmentCamera);
            GameObject alignmentCamera;

            if (alignmentCameraTransform == null)
            {
                Plugin.LogSource.LogDebug("AlignmentCamera not found in FactoryLayout, creating new one.");
                alignmentCamera = new GameObject(MenuOverhaulConstants.Environment.AlignmentCamera);
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
            if (envObjects == null || envObjects.EnvironmentUISceneFactory == null || envObjects.FactoryLayout == null || envObjects.EnvironmentUI == null)
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

            foreach (var sprite in SpriteCache.Values)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }
            SpriteCache.Clear();
        }

        private static Sprite LoadIconSprite(string baseFileName)
        {
            if (string.IsNullOrEmpty(baseFileName))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(baseFileName, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D iconTexture = LoadTextureFromDirectory(IconsDirectory, baseFileName);
            if (iconTexture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f), 100f);
            SpriteCache[baseFileName] = sprite;
            return sprite;
        }

        private static Texture2D LoadTextureFromDirectory(string directory, string baseFileName)
        {
            string filePath = ResolveAssetPath(directory, baseFileName);
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(fileBytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    Plugin.LogSource.LogWarning($"Failed to decode image file '{filePath}'.");
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(filePath);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                return texture;
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Failed to read texture from '{filePath}': {ex.Message}");
                return null;
            }
        }

        private static string ResolveAssetPath(string directory, string baseFileName)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName) || !Directory.Exists(directory))
            {
                return null;
            }

            string[] extensions = new[] { ".png", ".jpg", ".jpeg", ".tga" };

            if (Path.HasExtension(baseFileName))
            {
                string explicitPath = Path.Combine(directory, baseFileName);
                if (File.Exists(explicitPath))
                {
                    return explicitPath;
                }
            }

            foreach (string extension in extensions)
            {
                string candidatePath = Path.Combine(directory, baseFileName + extension);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            string[] matchingFiles = Directory.GetFiles(directory, baseFileName + ".*");
            if (matchingFiles.Length > 0)
            {
                return matchingFiles[0];
            }

            return null;
        }

        public static void DisposeResources()
        {
            ClearTextureCache();
            isAlignmentCameraMoved = false;
            Plugin.LogSource.LogDebug("LayoutHelpers resources disposed");
        }

    }
}