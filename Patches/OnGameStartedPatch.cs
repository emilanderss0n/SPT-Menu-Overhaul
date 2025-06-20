using EFT;
using MoxoPixel.MenuOverhaul.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {           
            // Set the game state in LightHelpers to track that we're in a game
            LightHelpers.SetGameStarted(true);
            
            // Handle the player model first - hide it
            if (PlayerProfileFeaturesPatch.clonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.clonedPlayerModelView.SetActive(false);
            }

            // Create instances of the patches we need to disable and clean up
            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            PlayerProfileFeaturesPatch profilePatch = new PlayerProfileFeaturesPatch();

            // Properly clean up event subscriptions before disabling patches
            menuPatch.CleanupBeforeDisable();
            profilePatch.CleanupBeforeDisable();

            // Clean up all GameObjects created by the mod
            LayoutHelpers.CleanupGameObjects();
            
            // Explicitly find and disable panorama and custom plane to ensure they're inactive during gameplay
            EnsureUIElementsDisabled();
            
            // Now disable the patches
            menuPatch.Disable();
            new SetAlphaPatch().Disable();
            new TweenButtonPatch().Disable();
            profilePatch.Disable();
            
            // After disabling patches, we can clean up memory resources
            // Using partial cleanup to allow quicker restoration when game ends
            LightHelpers.Cleanup();

            Plugin.LogSource.LogDebug("Menu overhaul patches and GameObjects disabled and cleaned up on game start");
        }
        
        private static void EnsureUIElementsDisabled()
        {
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
            if (environmentObjects?.FactoryLayout == null)
            {
                return;
            }
            
            // Double-check that panorama is disabled
            GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null && panorama.activeSelf)
            {
                panorama.SetActive(false);
            }
            
            // Double-check that CustomPlane is disabled
            GameObject customPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (customPlane != null && customPlane.activeSelf) 
            {
                customPlane.SetActive(false);
            }
            
            // Ensure decal_plane and its children are disabled
            GameObject decalPlane = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane != null)
            {
                if (decalPlane.activeSelf)
                {
                    decalPlane.SetActive(false);
                }
                
                // Also ensure child objects are disabled - decal_plane_pve
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null && pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(false);
                }
                
                // Also ensure child objects are disabled - decal_plane
                Transform decalPlaneChildTransform = decalPlane.transform.Find("decal_plane");
                if (decalPlaneChildTransform != null && decalPlaneChildTransform.gameObject.activeSelf)
                {
                    decalPlaneChildTransform.gameObject.SetActive(false);
                }
            }
        }
    }
}