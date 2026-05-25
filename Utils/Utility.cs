using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;

namespace MoxoPixel.MenuOverhaul.Utils
{
    public static class Utility
    {
        private static bool isInGame;

        /// <summary>
        /// Method to track when the game starts or ends
        /// </summary>
        public static void SetGameStarted(bool started)
        {
            isInGame = started;
        }

        /// <summary>
        /// Returns whether the game is currently in progress
        /// </summary>
        public static bool IsInGame()
        {
            return isInGame;
        }

        /// <summary>
        /// Configure the decal plane with the specified visibility
        /// </summary>
        public static void ConfigureDecalPlane(bool enable)
        {
            var env = LayoutHelpers.FindEnvironmentObjects();
            if (env?.FactoryLayout == null) return;

            GameObject decalPlane = env.FactoryLayout.transform.Find("decal_plane")?.gameObject;
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
        /// Set the position of the decal plane
        /// </summary>
        public static void SetDecalPlanePosition(float xPosition)
        {
            var env = LayoutHelpers.FindEnvironmentObjects();
            if (env?.FactoryLayout == null) return;

            GameObject decalPlane = env.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane == null || !decalPlane.activeSelf) return;

            decalPlane.transform.position = new Vector3(xPosition, -999.4f, 0f);
        }

        /// <summary>
        /// Reset game state tracking when cleaning up
        /// </summary>
        public static void ResetGameState()
        {
            isInGame = false;
            Plugin.LogSource.LogDebug("Game state tracking reset");
        }
    }
}