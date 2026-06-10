using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;
using EFT.UI.Screens;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;

namespace MoxoPixel.MenuOverhaul.Utils
{
    public static class GameStateUtility
    {
        private static bool isInGame;
        private const float DefaultDecalPlaneY = -999.4f;

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
            var env = MainMenuLayoutRuntime.FindEnvironmentObjects();
            if (env == null || env.FactoryLayout == null) return;

            Transform decalPlaneTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.DecalPlane);
            GameObject decalPlane = decalPlaneTransform != null ? decalPlaneTransform.gameObject : null;
            if (decalPlane == null) return;

            if (enable)
            {
                if (!decalPlane.activeSelf)
                {
                    decalPlane.SetActive(true);
                }

                Transform pveTransform = decalPlane.transform.Find(MenuOverhaulConstants.Environment.DecalPlanePve);
                if (pveTransform != null && !pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(true);
                }

                Transform childDecalPlane = decalPlane.transform.Find(MenuOverhaulConstants.Environment.DecalPlane);
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

                Transform pveTransform = decalPlane.transform.Find(MenuOverhaulConstants.Environment.DecalPlanePve);
                if (pveTransform != null && pveTransform.gameObject.activeSelf)
                {
                    pveTransform.gameObject.SetActive(false);
                }

                Transform childDecalPlane = decalPlane.transform.Find(MenuOverhaulConstants.Environment.DecalPlane);
                if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                {
                    childDecalPlane.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Set the position of the decal plane
        /// </summary>
        public static void SetDecalPlanePosition(float xPosition, float yOffset)
        {
            var env = MainMenuLayoutRuntime.FindEnvironmentObjects();
            if (env == null || env.FactoryLayout == null) return;

            Transform decalPlaneTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.DecalPlane);
            GameObject decalPlane = decalPlaneTransform != null ? decalPlaneTransform.gameObject : null;
            if (decalPlane == null || !decalPlane.activeSelf) return;

            decalPlane.transform.position = new Vector3(xPosition, DefaultDecalPlaneY + yOffset, 0f);
        }

        /// <summary>
        /// Reset game state tracking when cleaning up
        /// </summary>
        public static void ResetGameState()
        {
            isInGame = false;
            MenuDiagnosticsLogger.Debug(LogSubsystem.General, "Game state tracking reset");
        }
    }
}