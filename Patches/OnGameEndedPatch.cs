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
            // Reset the game state in LightHelpers to indicate we're no longer in a game
            LightHelpers.SetGameStarted(false);
            
            // Re-enable all patches
            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            menuPatch.Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();
            new PlayerProfileFeaturesPatch().Enable();

            // If the player model was created before, make it visible again
            if (PlayerProfileFeaturesPatch.clonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.clonedPlayerModelView.SetActive(true);
            }
            
            // Handle any active scene to reinitialize UI elements
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
                
                // Reset the original hierarchy state
                ResetOriginalState(environmentObjects);
                
                // Now rebuild our custom elements in the correct order
                RebuildCustomElements(environmentObjects);

                // Force a check of decal_plane_pve visibility as a final check
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
                // First check if parent is active
                if (!decalPlane.activeSelf)
                {
                    decalPlane.SetActive(true);
                }
                
                // Now check decal_plane_pve
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
                
                // Make sure child decal_plane stays disabled
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
            // Reactivate all original elements first
            if (environmentObjects.FactoryLayout != null)
            {
                // DO NOT re-enable panorama - we want it to stay disabled
                GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
                if (panorama != null)
                {
                    panorama.SetActive(false);
                }

                // Reset and reactivate decal_plane
                GameObject decalPlaneObject = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
                if (decalPlaneObject != null)
                {
                    // Make sure it's active
                    decalPlaneObject.SetActive(true);
                    
                    // Reset its position
                    decalPlaneObject.transform.position = new Vector3(0f, -999.4f, 0f);
                    
                    // Explicitly activate decal_plane_pve child here to ensure it's active
                    Transform pveTransform = decalPlaneObject.transform.Find("decal_plane_pve");
                    if (pveTransform != null)
                    {
                        pveTransform.gameObject.SetActive(true); 
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("ResetOriginalState - Could not find decal_plane_pve child GameObject");
                    }
                    
                    // Make sure child decal_plane is still disabled
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

                // Reset LampContainer
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
                // Ensure it's active first
                if (!decalPlaneObject.activeSelf)
                {
                    decalPlaneObject.SetActive(true);
                }
                
                // Now configure it
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

            // Ensure the game object is active
            GameObject decalPlaneObject = decalPlaneTransform.gameObject;
            if (!decalPlaneObject.activeSelf)
            {
                decalPlaneObject.SetActive(true);
            }

            // Set position according to user settings
            decalPlaneTransform.position = new Vector3(Utils.Settings.PositionLogotypeHorizontal.Value, -999.4f, 0f);
            
            // Configure materials with emission
            Renderer decalRenderer = decalPlaneTransform.GetComponent<Renderer>();
            if (decalRenderer != null)
            {
                foreach (Material mat in decalRenderer.materials)
                {
                    if (mat != null)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.white);
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("ConfigureDecalPlane - No Renderer component found on decal_plane");
            }

            // Configure only specific child objects
            // Configure the decal_plane_pve child object - THIS SHOULD BE ACTIVE
            Transform pveTransform = decalPlaneObject.transform.Find("decal_plane_pve");
            if (pveTransform != null)
            {
                pveTransform.gameObject.SetActive(true);
            }
            else
            {
                Plugin.LogSource.LogWarning("ConfigureDecalPlane - Could not find decal_plane_pve child object");
            }

            // The child decal_plane should remain INACTIVE (both during and after game)
            Transform decalPlaneChildTransform = decalPlaneObject.transform.Find("decal_plane");
            if (decalPlaneChildTransform != null)
            {
                decalPlaneChildTransform.gameObject.SetActive(false);
            }
        }
    }
}