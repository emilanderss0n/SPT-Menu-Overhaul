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
            Utility.SetGameStarted(false);
            
            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            menuPatch.Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();
            new PlayerProfileFeaturesPatch().Enable();

            if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(true);
                LightHelpers.SetupLights(PlayerProfileFeaturesPatch.ClonedPlayerModelView);
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

                Utility.ConfigureDecalPlane(true);
                Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value);

                Plugin.LogSource.LogDebug("Menu UI elements successfully restored after game session end");
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"Error during menu restoration: {ex.Message}\n{ex.StackTrace}");
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

                Utility.ConfigureDecalPlane(true);
                Utility.SetDecalPlanePosition(0f);

                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "LampContainer", false);

                GameObject existingCustomPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
                if (existingCustomPlane != null)
                {
                    Object.Destroy(existingCustomPlane);
                }
            }

            if (environmentObjects.EnvironmentUISceneFactory != null)
            {
                GameObject factoryCameraContainer = environmentObjects.EnvironmentUISceneFactory.transform.Find("FactoryCameraContainer")?.gameObject;
                if (factoryCameraContainer != null)
                {
                    GameObject mainMenuCamera = factoryCameraContainer.transform.Find("MainMenuCamera")?.gameObject;
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

            Utility.ConfigureDecalPlane(true);
            Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value);

            GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null && panorama.activeSelf)
            {
                panorama.SetActive(false);
            }

            LayoutHelpers.SetPanoramaEmissionMap(environmentObjects.FactoryLayout, true);

            LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "LampContainer", true);
            LightHelpers.UpdateLights();

            GameObject customPlane = LayoutHelpers.GetBackgroundPlane();
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
                customPlane.transform.localScale = new Vector3(Settings.ScaleBackgroundX.Value, 1f, Settings.ScaleBackgroundY.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("RebuildCustomElements - CustomPlane not found after recreation attempt");
            }

            MenuOverhaulPatch.UpdateLayoutElements();
        }
    }
}