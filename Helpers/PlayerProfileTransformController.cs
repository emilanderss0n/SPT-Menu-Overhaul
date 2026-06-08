using MoxoPixel.MenuOverhaul.Utils;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class PlayerProfileTransformController
    {
        private const float BottomFieldBaseScale = 0.6f;
        private const float HighQualityPreviewSupersampleFactor = 1.6f;

        public static Transform GetBottomFieldTransform(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                return null;
            }

            return clonedPlayerModelView.transform.Find(MenuOverhaulConstants.PlayerModel.BottomFieldName);
        }

        public static void UpdatePlayerModelPosition(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelPosition - clonedPlayerModelView is null.");
                return;
            }

            clonedPlayerModelView.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -150f, 0f);

            if (!Settings.EnableLargerPlayerModel.Value)
            {
                return;
            }

            RectTransform rectTransform = clonedPlayerModelView.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            float horizontalPos = Settings.PositionPlayerModelHorizontal.Value;
            rectTransform.anchoredPosition = new Vector2(horizontalPos, 0f);
            rectTransform.offsetMax = new Vector2(horizontalPos, 0f);
            rectTransform.offsetMin = new Vector2(horizontalPos, 0f);
            rectTransform.localPosition = new Vector3(horizontalPos, 0f, 0f);
        }

        public static PlayerModelDragRotator UpdatePlayerModelRotation(GameObject clonedPlayerModelView, PlayerModelDragRotator dragRotator)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelRotation - clonedPlayerModelView is null.");
                return dragRotator;
            }

            Transform target = clonedPlayerModelView.transform.Find(MenuOverhaulConstants.PlayerModel.MenuPlayerPath);
            if (target == null)
            {
                return dragRotator;
            }

            if (dragRotator == null)
            {
                dragRotator = clonedPlayerModelView.GetComponent<PlayerModelDragRotator>();
                if (dragRotator == null)
                {
                    dragRotator = clonedPlayerModelView.AddComponent<PlayerModelDragRotator>();
                }
            }

            dragRotator.Target = target;
            dragRotator.SetYaw(Settings.RotationPlayerModelHorizontal.Value);
            return dragRotator;
        }

        public static void UpdateCameraPosition(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("UpdateCameraPosition - clonedPlayerModelView is null.");
                return;
            }

            Transform cameraTransform = clonedPlayerModelView.transform.Find(MenuOverhaulConstants.PlayerModel.CameraInventoryPath);
            if (cameraTransform == null)
            {
                Plugin.LogSource.LogWarning($"UpdateCameraPosition - {MenuOverhaulConstants.PlayerModel.CameraInventoryPath} not found.");
                return;
            }

            Vector3 basePosition = Settings.EnableLargerPlayerModel.Value
                ? new Vector3(0f, 0.2f, 1.5f)
                : new Vector3(0f, 0f, 1f);

            Vector3 configuredOffset = new Vector3(
                Settings.CameraInventoryPositionX.Value,
                Settings.CameraInventoryPositionY.Value,
                Settings.CameraInventoryPositionZ.Value);

            cameraTransform.localPosition = basePosition + configuredOffset;
        }

        public static void UpdateCameraRotation(GameObject clonedPlayerModelView)
        {
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("UpdateCameraRotation - clonedPlayerModelView is null.");
                return;
            }

            Transform cameraTransform = clonedPlayerModelView.transform.Find(MenuOverhaulConstants.PlayerModel.CameraInventoryPath);
            if (cameraTransform == null)
            {
                Plugin.LogSource.LogWarning($"UpdateCameraRotation - {MenuOverhaulConstants.PlayerModel.CameraInventoryPath} not found.");
                return;
            }

            cameraTransform.localRotation = Quaternion.Euler(
                Settings.CameraInventoryRotationX.Value,
                Settings.CameraInventoryRotationY.Value,
                Settings.CameraInventoryRotationZ.Value);
        }

        public static void UpdateBottomFieldPosition(GameObject clonedPlayerModelView)
        {
            Transform bottomFieldTransform = GetBottomFieldTransform(clonedPlayerModelView);
            if (bottomFieldTransform == null)
            {
                Plugin.LogSource.LogWarning($"BottomFieldPositionChanged - {MenuOverhaulConstants.PlayerModel.BottomFieldName} transform not found.");
                return;
            }

            bottomFieldTransform.localPosition = new Vector3(
                Settings.PositionBottomFieldHorizontal.Value,
                Settings.PositionBottomFieldVertical.Value,
                0f);
        }

        public static void UpdateBottomFieldScale(GameObject clonedPlayerModelView)
        {
            Transform bottomFieldTransform = GetBottomFieldTransform(clonedPlayerModelView);
            if (bottomFieldTransform == null)
            {
                return;
            }

            float compensatedBottomFieldScale = BottomFieldBaseScale * GetPreviewSupersampleFactor();
            bottomFieldTransform.localScale = new Vector3(compensatedBottomFieldScale, compensatedBottomFieldScale, compensatedBottomFieldScale);
        }

        public static void HidePlayerModelExtraElements(GameObject modelInstance)
        {
            if (modelInstance == null)
            {
                Plugin.LogSource.LogWarning("HidePlayerModelExtraElements - modelInstance is null.");
                return;
            }

            LayoutHelpers.SetChildActive(modelInstance, "IconsContainer", false);
            LayoutHelpers.SetChildActive(modelInstance, "DragTrigger", false);
        }

        public static void AdjustInnerPlayerModelPosition(GameObject modelInstance)
        {
            if (modelInstance == null)
            {
                Plugin.LogSource.LogWarning("AdjustInnerPlayerModelPosition - modelInstance is null.");
                return;
            }

            Transform playerMvObject = modelInstance.transform.Find(MenuOverhaulConstants.PlayerModel.RootPath);
            if (playerMvObject == null)
            {
                Plugin.LogSource.LogWarning("AdjustInnerPlayerModelPosition - PlayerMVObject not found in modelInstance.");
                return;
            }

            Transform innerModelTransform = playerMvObject.Find(MenuOverhaulConstants.PlayerModel.MenuPlayerName);
            if (innerModelTransform == null)
            {
                Plugin.LogSource.LogWarning($"AdjustInnerPlayerModelPosition - {MenuOverhaulConstants.PlayerModel.MenuPlayerName} not found in {MenuOverhaulConstants.PlayerModel.RootPath}.");
                return;
            }

            innerModelTransform.localPosition = new Vector3(0f, -1.1f, 5f);
        }

        public static float GetPreviewSupersampleFactor()
        {
            return Settings.EnableHighQualityPlayerPreview.Value ? HighQualityPreviewSupersampleFactor : 1f;
        }
    }
}
