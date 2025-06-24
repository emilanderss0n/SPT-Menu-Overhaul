using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class OnGameEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod(nameof(Player.OnGameSessionEnd), BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            LightHelpers.SetGameStarted(false);
            
            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            menuPatch.Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();
            new PlayerProfileFeaturesPatch().Enable();

            if (PlayerProfileFeaturesPatch.clonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.clonedPlayerModelView.SetActive(true);
            }
            
            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == "CommonUIScene")
            {
                RestoreMenuUIElements();
            }

            Plugin.LogSource.LogDebug("Menu overhaul patches and GameObjects re-enabled after game session end");
        }
        
        private static void RestoreMenuUIElements()
        {
            try
            {
                var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
                if (environmentObjects?.FactoryLayout == null)
                {
                    Plugin.LogSource.LogError("RestoreMenuUIElements - Could not find environment objects or FactoryLayout is null.");
                    return;
                }
                
                ResetOriginalState(environmentObjects);
                RebuildCustomElements(environmentObjects);
                ForceCheckDecalPlaneVisibility(environmentObjects);

                Plugin.LogSource.LogDebug("Menu UI elements successfully restored after game session end");
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"Error during menu restoration: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ForceCheckDecalPlaneVisibility(LayoutHelpers.EnvironmentObjects environmentObjects)
        {
            if (environmentObjects?.FactoryLayout == null) return;
            
            GameObject decalPlane = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane != null)
            {
                if (!decalPlane.activeSelf)
                {
                    decalPlane.SetActive(true);
                }
                
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null)
                {
                    if (!pveTransform.gameObject.activeSelf)
                    {
                        pveTransform.gameObject.SetActive(true);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("ForceCheckDecalPlaneVisibility - Could not find decal_plane_pve child object");
                }
                
                Transform childDecalPlane = decalPlane.transform.Find("decal_plane");
                if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                {
                    childDecalPlane.gameObject.SetActive(false);
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("ForceCheckDecalPlaneVisibility - Could not find decal_plane GameObject");
            }
        }

        private static void ResetOriginalState(LayoutHelpers.EnvironmentObjects environmentObjects)
        {
            if (environmentObjects.FactoryLayout != null)
            {
                GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
                if (panorama != null)
                {
                    panorama.SetActive(false);
                }

                GameObject decalPlaneObject = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
                if (decalPlaneObject != null)
                {
                    decalPlaneObject.SetActive(true);
                    decalPlaneObject.transform.position = new Vector3(0f, -999.4f, 0f);
                    
                    Transform pveTransform = decalPlaneObject.transform.Find("decal_plane_pve");
                    if (pveTransform != null)
                    {
                        pveTransform.gameObject.SetActive(true); 
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("ResetOriginalState - Could not find decal_plane_pve child GameObject");
                    }
                    
                    Transform decalPlaneChildTransform = decalPlaneObject.transform.Find("decal_plane");
                    if (decalPlaneChildTransform != null)
                    {
                        decalPlaneChildTransform.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("ResetOriginalState - decal_plane GameObject not found");
                }

                GameObject lampContainer = environmentObjects.FactoryLayout.transform.Find("LampContainer")?.gameObject;
                if (lampContainer != null)
                {
                    lampContainer.SetActive(false);
                }

                // Clean up any existing CustomPlane to let it be recreated properly
                GameObject existingCustomPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
                if (existingCustomPlane != null)
                {
                    UnityEngine.Object.Destroy(existingCustomPlane);
                }
            }

            // Reset FactoryCameraContainer
            if (environmentObjects.EnvironmentUISceneFactory != null)
            {
                var factoryCameraContainer = environmentObjects.EnvironmentUISceneFactory.transform.Find("FactoryCameraContainer");
                if (factoryCameraContainer != null)
                {
                    GameObject mainMenuCamera = factoryCameraContainer.Find("MainMenuCamera")?.gameObject;
                    if (mainMenuCamera != null)
                    {
                        mainMenuCamera.SetActive(true);
                    }
                }
            }
        }

        private static void RebuildCustomElements(LayoutHelpers.EnvironmentObjects environmentObjects)
        {
            if (environmentObjects?.FactoryLayout == null)
            {
                Plugin.LogSource.LogWarning("RebuildCustomElements - FactoryLayout is null");
                return;
            }

            // Step 1: Configure decal_plane - make sure it exists and is active
            GameObject decalPlaneObject = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlaneObject != null)
            {
                if (!decalPlaneObject.activeSelf)
                {
                    decalPlaneObject.SetActive(true);
                }
                
                ConfigureDecalPlane(decalPlaneObject.transform);
            }
            else
            {
                Plugin.LogSource.LogWarning("RebuildCustomElements - decal_plane GameObject not found");
            }

            // Step 2: Make sure panorama is disabled
            GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null)
            {
                if (panorama.activeSelf)
                {
                    panorama.SetActive(false);
                }
            }

            // Step 3: Force recreation of custom plane with materials
            LayoutHelpers.SetPanoramaEmissionMap(environmentObjects.FactoryLayout, true);

            // Step 4: Setup lights and lamp container
            LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "LampContainer", true);
            LightHelpers.UpdateLights();

            // Step 5: Make sure CustomPlane is active with proper settings
            GameObject customPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
                customPlane.transform.localScale = new Vector3(Settings.scaleBackgroundX.Value, 1f, Settings.scaleBackgroundY.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("RebuildCustomElements - CustomPlane not found after recreation attempt");
            }

            // Step 6: Final layout update
            MenuOverhaulPatch.UpdateLayoutElements();
        }

        private static void ConfigureDecalPlane(Transform decalPlaneTransform)
        {
            if (decalPlaneTransform == null)
            {
                Plugin.LogSource.LogWarning("ConfigureDecalPlane - decalPlaneTransform is null");
                return;
            }

            GameObject decalPlaneObject = decalPlaneTransform.gameObject;
            if (!decalPlaneObject.activeSelf)
            {
                decalPlaneObject.SetActive(true);
            }

            decalPlaneTransform.position = new Vector3(Utils.Settings.PositionLogotypeHorizontal.Value, -999.4f, 0f);

            Transform pveTransform = decalPlaneObject.transform.Find("decal_plane_pve");
            if (pveTransform != null)
            {
                pveTransform.gameObject.SetActive(true);
            }
            else
            {
                Plugin.LogSource.LogWarning("ConfigureDecalPlane - Could not find decal_plane_pve child object");
            }

            Transform decalPlaneChildTransform = decalPlaneObject.transform.Find("decal_plane");
            if (decalPlaneChildTransform != null)
            {
                decalPlaneChildTransform.gameObject.SetActive(false);
            }
        }
    }
}