using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;

namespace MoxoPixel.MenuOverhaul.Utils
{
    public static class Utility
    {
        private static bool isInGame;
        
        private static LayoutHelpers.EnvironmentObjects cachedEnvironmentObjects;
        private static GameObject cachedDecalPlane;

        /// <summary>
        /// Method to track when the game starts or ends
        /// </summary>
        public static void SetGameStarted(bool started)
        {
            isInGame = started;
            
            if (started)
            {
                DisableDecalPlaneIfInGame();
            }
        }

        /// <summary>
        /// Returns whether the game is currently in progress
        /// </summary>
        public static bool IsInGame()
        {
            return isInGame;
        }
        
        /// <summary>
        /// Get or find the decal plane GameObject
        /// </summary>
        /// <returns>The decal plane GameObject or null if not found</returns>
        private static GameObject GetDecalPlane()
        {
            if (cachedDecalPlane != null)
            {
                return cachedDecalPlane;
            }
            
            cachedEnvironmentObjects ??= LayoutHelpers.FindEnvironmentObjects();
            
            if (cachedEnvironmentObjects?.FactoryLayout == null)
            {
                return null;
            }
            
            cachedDecalPlane = cachedEnvironmentObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            return cachedDecalPlane;
        }

        /// <summary>
        /// Configure the decal plane with the specified visibility
        /// </summary>
        public static void ConfigureDecalPlane(bool enable)
        {
            GameObject decalPlane = GetDecalPlane();
            if (decalPlane == null) return;
            
            if (enable)
            {
                if (!decalPlane.activeSelf)
                {
                    decalPlane.SetActive(true);
                }
                
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null && !pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(true);
                }
                
                Transform childDecalPlane = decalPlane.transform.Find("decal_plane");
                if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                {
                    childDecalPlane.gameObject.SetActive(false);
                }
            }
            else
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
                
                Transform childDecalPlane = decalPlane.transform.Find("decal_plane");
                if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                {
                    childDecalPlane.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Enhanced method to ensure decal_plane is disabled during gameplay, even in pause menu
        /// </summary>
        public static void DisableDecalPlaneIfInGame()
        {
            if (!isInGame) 
            {
                return;
            }
            
            ConfigureDecalPlane(false);
        }
        
        /// <summary>
        /// Set the position of the decal plane
        /// </summary>
        public static void SetDecalPlanePosition(float xPosition)
        {
            GameObject decalPlane = GetDecalPlane();
            if (decalPlane == null || !decalPlane.activeSelf) return;
            
            decalPlane.transform.position = new Vector3(xPosition, -999.4f, 0f);
        }

        /// <summary>
        /// Reset game state tracking and cached objects when cleaning up
        /// </summary>
        public static void ResetGameState()
        {
            isInGame = false;
            cachedEnvironmentObjects = null;
            cachedDecalPlane = null;
            Plugin.LogSource.LogDebug("Game state tracking and cached objects reset");
        }
    }
}