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
            LightHelpers.SetGameStarted(true);
            
            if (PlayerProfileFeaturesPatch.clonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.clonedPlayerModelView.SetActive(false);
            }

            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            PlayerProfileFeaturesPatch profilePatch = new PlayerProfileFeaturesPatch();

            menuPatch.CleanupBeforeDisable();
            profilePatch.CleanupBeforeDisable();
            LayoutHelpers.CleanupGameObjects();
            EnsureUIElementsDisabled();
            
            menuPatch.Disable();
            new SetAlphaPatch().Disable();
            new TweenButtonPatch().Disable();
            profilePatch.Disable();
            
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
            
            GameObject panorama = environmentObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null && panorama.activeSelf)
            {
                panorama.SetActive(false);
            }
            
            GameObject customPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (customPlane != null && customPlane.activeSelf) 
            {
                customPlane.SetActive(false);
            }
            
            GameObject decalPlane = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane != null)
            {
                if (decalPlane.activeSelf)
                {
                    decalPlane.SetActive(false);
                }
                
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null && pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(false);
                }
                
                Transform decalPlaneChildTransform = decalPlane.transform.Find("decal_plane");
                if (decalPlaneChildTransform != null && decalPlaneChildTransform.gameObject.activeSelf)
                {
                    decalPlaneChildTransform.gameObject.SetActive(false);
                }
            }
        }
    }
}