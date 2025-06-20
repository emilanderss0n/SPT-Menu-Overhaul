using UnityEngine;
using MoxoPixel.MenuOverhaul.Utils;
using System;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class LightHelpers
    {
        // Store Light components directly instead of Transforms to avoid repeated GetComponent calls.
        private static Light mainLightComponent;
        private static Light hairLightComponent;
        
        // Add tracking for game state to help manage decal_plane visibility
        private static bool isInGame = false;

        public static void SetupLights(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("SetupLights - clonedPlayerModelView is null.");
                return;
            }
            mainLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Main Light", ConfigureMainLight);
            hairLightComponent = FindAndSetupLight(clonedPlayerModelView, "PlayerMVObject/PlayerMVObjectLights/Hair Light", ConfigureHairLight);
        }

        // Renamed and refactored for clarity and direct Light component handling
        private static Light FindAndSetupLight(GameObject parent, string path, Action<Light> configureAction)
        {
            if (parent == null || string.IsNullOrEmpty(path) || configureAction == null)
            {
                Plugin.LogSource.LogError("FindAndSetupLight - Invalid arguments.");
                return null;
            }

            Transform lightTransform = parent.transform.Find(path);
            if (lightTransform == null)
            {
                Plugin.LogSource.LogWarning($"Light GameObject not found at path: {path} in {parent.name}.");
                return null;
            }

            Light lightComponent = lightTransform.GetComponent<Light>();
            if (lightComponent == null)
            {
                Plugin.LogSource.LogWarning($"Light component not found on GameObject at path: {path} in {parent.name}.");
                return null;
            }

            configureAction(lightComponent);
            return lightComponent;
        }

        // Renamed for clarity
        private static void ConfigureMainLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            // Add any other specific configurations for the main light here
        }

        // Renamed for clarity
        private static void ConfigureHairLight(Light light)
        {
            if (light == null) return;
            light.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            // Add any other specific configurations for the hair light here
        }

        public static void UpdateLights()
        {
            // Update shadows based on current settings
            UpdateSingleLightShadows(mainLightComponent);
            UpdateSingleLightShadows(hairLightComponent);
            
            // Only disable decal plane when we're actually in a game
            if (isInGame)
            {
                DisableDecalPlaneIfInGame();
            }
        }

        // Method to track when the game starts
        public static void SetGameStarted(bool started)
        {
            bool wasInGame = isInGame;
            isInGame = started;
            
            if (started)
            {
                DisableDecalPlaneIfInGame();
            }
        }

        // Enhanced method to ensure decal_plane is disabled during gameplay, even in pause menu
        public static void DisableDecalPlaneIfInGame()
        {
            if (!isInGame) 
            {
                return;
            }
            
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
            if (environmentObjects?.FactoryLayout == null) return;
            
            // Disable parent decal_plane object
            GameObject decalPlane = environmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane != null)
            {
                // First check the main decal_plane
                if (decalPlane.activeSelf)
                {
                    decalPlane.SetActive(false);
                }
                
                // Now check and disable the child objects - decal_plane_pve
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null && pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(false);
                }
                
                // Check and disable the decal_plane child object
                Transform decalPlaneChildTransform = decalPlane.transform.Find("decal_plane");
                if (decalPlaneChildTransform != null && decalPlaneChildTransform.gameObject.activeSelf)
                {
                    decalPlaneChildTransform.gameObject.SetActive(false);
                }
            }
        }

        // Renamed for clarity
        private static void UpdateSingleLightShadows(Light lightComponent)
        {
            if (lightComponent != null)
            {
                lightComponent.shadows = Settings.EnableExtraShadows.Value ? LightShadows.Soft : LightShadows.None;
            }
            // else, the light was not found during setup, warning already logged.
        }

        public static void Cleanup()
        {
            // Clear static references to lights for garbage collection
            mainLightComponent = null;
            hairLightComponent = null;
            
            // Reset game state tracking
            isInGame = false;
            
            Plugin.LogSource.LogDebug("Light helper references cleared during cleanup");
        }
    }
}