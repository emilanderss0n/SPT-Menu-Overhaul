using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;
using EFT;
using SPT.Reflection.Utils;
using Object = UnityEngine.Object;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class PlayerProfileFeaturesPatch : ModulePatch, ICleanupPatch
    {
        private static bool menuPlayerCreated;
        public static GameObject ClonedPlayerModelView;

        private static bool _profileSettingsSubscribed;
        private static bool _experienceEventsSubscribed;
        private static Vector2? _basePreviewViewportSize;
        private static Vector3? _basePreviewViewportScale;

        // Mouse drag rotation handle for the cloned player model.
        private static PlayerModelDragRotator _dragRotator;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(MenuOverhaulConstants.Reflection.MenuScreenShowMethod, [typeof(Profile), typeof(MatchmakerPlayerControllerClass), typeof(ESessionMode)
            ]);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance, Profile profile, MatchmakerPlayerControllerClass matchmaker)
        {
            try
            {
                if (__instance == null)
                {
                    Plugin.LogSource.LogWarning("MenuScreen instance is null.");
                    return;
                }

                // Only show the custom player model on the actual main menu screen.
                // The in-raid Disconnect/Resume menu and the reconnect menu invoke Show with
                // null profile/matchmaker; skip those so the default game UI is preserved.
                if (profile == null || matchmaker == null || Utility.IsInGame())
                {
                    return;
                }

                await AddPlayerModel();
                SubscribeToProfileSettingsChanges();
                SubscribeToCharacterLevelUpEvent();

                if (ClonedPlayerModelView != null)
                {
                    UpdatePlayerModelPosition();
                    UpdatePlayerModelRotation();
                    UpdateCameraPosition();
                    UpdateCameraRotation();
                    BottomFieldPositionChanged();
                    UpdateTextColors();
                }
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError(e.ToString());
            }
        }

        private static void SubscribeToProfileSettingsChanges()
        {
            if (_profileSettingsSubscribed) return;

            Settings.PositionPlayerModelHorizontal.SettingChanged += OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged += OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged += OnBottomFieldPositionChanged;
            Settings.RotationPlayerModelHorizontal.SettingChanged += OnPlayerModelRotationChanged;
            Settings.AccentColor.SettingChanged += OnAccentColorChanged;
            Settings.EnableLargerPlayerModel.SettingChanged += OnLargerPlayerModelChanged;
            Settings.EnableHighQualityPlayerPreview.SettingChanged += OnPlayerPreviewQualityChanged;
            Settings.EnableDefaultPlayerAnimation.SettingChanged += OnPlayerPreviewAnimationChanged;
            Settings.CameraInventoryPositionX.SettingChanged += OnCameraInventoryTransformChanged;
            Settings.CameraInventoryPositionY.SettingChanged += OnCameraInventoryTransformChanged;
            Settings.CameraInventoryPositionZ.SettingChanged += OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationX.SettingChanged += OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationY.SettingChanged += OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationZ.SettingChanged += OnCameraInventoryTransformChanged;

            _profileSettingsSubscribed = true;
        }

        private static void UnsubscribeFromProfileSettingsChanges()
        {
            if (!_profileSettingsSubscribed) return;

            Settings.PositionPlayerModelHorizontal.SettingChanged -= OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged -= OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged -= OnBottomFieldPositionChanged;
            Settings.RotationPlayerModelHorizontal.SettingChanged -= OnPlayerModelRotationChanged;
            Settings.AccentColor.SettingChanged -= OnAccentColorChanged;
            Settings.EnableLargerPlayerModel.SettingChanged -= OnLargerPlayerModelChanged;
            Settings.EnableHighQualityPlayerPreview.SettingChanged -= OnPlayerPreviewQualityChanged;
            Settings.EnableDefaultPlayerAnimation.SettingChanged -= OnPlayerPreviewAnimationChanged;
            Settings.CameraInventoryPositionX.SettingChanged -= OnCameraInventoryTransformChanged;
            Settings.CameraInventoryPositionY.SettingChanged -= OnCameraInventoryTransformChanged;
            Settings.CameraInventoryPositionZ.SettingChanged -= OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationX.SettingChanged -= OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationY.SettingChanged -= OnCameraInventoryTransformChanged;
            Settings.CameraInventoryRotationZ.SettingChanged -= OnCameraInventoryTransformChanged;

            _profileSettingsSubscribed = false;
        }

        private static void OnPlayerModelPositionChanged(object sender, EventArgs e) => UpdatePlayerModelPosition();
        private static void OnPlayerModelRotationChanged(object sender, EventArgs e) => UpdatePlayerModelRotation();
        private static void OnBottomFieldPositionChanged(object sender, EventArgs e) => BottomFieldPositionChanged();
        private static void OnAccentColorChanged(object sender, EventArgs e)
        {
            UpdateTextColors();
            LightHelpers.UpdateAccentLightColor();
        }
        private static void OnLargerPlayerModelChanged(object sender, EventArgs e)
        {
            UpdatePlayerModelPosition();
            UpdateCameraPosition();
            UpdateCameraRotation();
            ConfigurePreviewRenderQuality(ClonedPlayerModelView);
        }
        private static void OnPlayerPreviewQualityChanged(object sender, EventArgs e)
        {
            ConfigurePreviewRenderQuality(ClonedPlayerModelView);
            UpdateBottomFieldScale();
        }

        private static async void OnPlayerPreviewAnimationChanged(object sender, EventArgs e)
        {
            await RefreshPlayerModel();
            UpdatePlayerModelRotation();
        }

        private static void OnCameraInventoryTransformChanged(object sender, EventArgs e)
        {
            UpdateCameraPosition();
            UpdateCameraRotation();
        }

        private static void UpdateTextColors()
        {
            PlayerProfileStatsController.UpdateTextColors(ClonedPlayerModelView);
        }

        private static void UpdatePlayerModelPosition()
        {
            PlayerProfileTransformController.UpdatePlayerModelPosition(ClonedPlayerModelView);
        }

        private static void UpdatePlayerModelRotation()
        {
            _dragRotator = PlayerProfileTransformController.UpdatePlayerModelRotation(ClonedPlayerModelView, _dragRotator);
        }

        private static void UpdateCameraPosition()
        {
            PlayerProfileTransformController.UpdateCameraPosition(ClonedPlayerModelView);
        }

        private static void UpdateCameraRotation()
        {
            PlayerProfileTransformController.UpdateCameraRotation(ClonedPlayerModelView);
        }

        private static void BottomFieldPositionChanged()
        {
            PlayerProfileTransformController.UpdateBottomFieldPosition(ClonedPlayerModelView);
        }

        private static void SubscribeToCharacterLevelUpEvent()
        {
            if (_experienceEventsSubscribed) return;
            if (PatchConstants.BackEndSession != null && PatchConstants.BackEndSession.Profile != null && PatchConstants.BackEndSession.Profile.Info != null)
            {
                PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged += OnExperienceChanged;
                _experienceEventsSubscribed = true;
            }
            else
            {
                Plugin.LogSource.LogWarning("SubscribeToCharacterLevelUpEvent - BackEndSession.Profile.Info is null.");
            }
        }

        private static void UnsubscribeFromCharacterLevelUpEvent()
        {
            if (!_experienceEventsSubscribed) return;
            if (PatchConstants.BackEndSession != null && PatchConstants.BackEndSession.Profile != null && PatchConstants.BackEndSession.Profile.Info != null)
            {
                PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged -= OnExperienceChanged;
                _experienceEventsSubscribed = false;
            }
        }

        private static void OnExperienceChanged(int oldExperience, int newExperience)
        {
            Transform bottomFieldTransform = PlayerProfileTransformController.GetBottomFieldTransform(ClonedPlayerModelView);
            if (bottomFieldTransform != null)
            {
                UpdatePlayerStats();
            }
            else
            {
                Plugin.LogSource.LogWarning("OnExperienceChanged - BottomField transform not found, cannot update player stats.");
            }
        }

        private static float GetPreviewSupersampleFactor()
        {
            return PlayerProfileTransformController.GetPreviewSupersampleFactor();
        }

        private static void UpdateBottomFieldScale()
        {
            PlayerProfileTransformController.UpdateBottomFieldScale(ClonedPlayerModelView);
        }

        private static async Task AddPlayerModel()
        {
            if (menuPlayerCreated && ClonedPlayerModelView != null)
            {
                await RefreshPlayerModel();
                return;
            }
            if (menuPlayerCreated)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - MenuPlayerCreated is true, but clonedPlayerModelView is null. Attempting to recreate.");
                menuPlayerCreated = false;
            }

            GameObject playerModelViewPrefab = GameObject.Find(MenuOverhaulConstants.PlayerModel.PlayerModelViewPrefabPath);
            GameObject menuScreenParent = GameObject.Find(MenuOverhaulConstants.MenuScreen.ScenePath);

            if (playerModelViewPrefab == null || menuScreenParent == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView prefab or MenuScreen parent not found. Cannot create player model.");
                return;
            }

            ClonedPlayerModelView = CreateClonedPlayerModelView(menuScreenParent, playerModelViewPrefab);
            if (ClonedPlayerModelView == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - Failed to create clonedPlayerModelView.");
                return;
            }

            menuPlayerCreated = true;

            PlayerModelView playerModelViewScript = ClonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView script not found on the cloned PlayerModelViewObject.");
                Object.Destroy(ClonedPlayerModelView);
                menuPlayerCreated = false;
                ClonedPlayerModelView = null;
                return;
            }

            ConfigurePlayerModelVisuals(ClonedPlayerModelView);

            GameObject playerLevelViewPrefab = GameObject.Find(MenuOverhaulConstants.PlayerModel.PlayerLevelPrefabPath);
            GameObject playerLevelIconViewPrefab = GameObject.Find(MenuOverhaulConstants.PlayerModel.PlayerLevelIconPrefabPath);
            if (playerLevelViewPrefab == null || playerLevelIconViewPrefab == null)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - PlayerLevelViewPrefab or PlayerLevelIconViewPrefab not found. BottomField setup might be incomplete.");
            }
            SetupBottomField(ClonedPlayerModelView, playerLevelViewPrefab, playerLevelIconViewPrefab);

            HidePlayerModelExtraElements(ClonedPlayerModelView);

            if (PatchConstants.BackEndSession?.Profile != null)
            {
                await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, Settings.EnableDefaultPlayerAnimation.Value);
                AdjustInnerPlayerModelPosition(ClonedPlayerModelView);
                ConfigurePreviewRenderQuality(ClonedPlayerModelView);
            }
            else
            {
                Plugin.LogSource.LogError("AddPlayerModel - BackEndSession.Profile is null. Cannot show player model view script.");
            }
        }

        private static GameObject CreateClonedPlayerModelView(GameObject parent, GameObject prefab)
        {
            if (parent == null || prefab == null)
            {
                Plugin.LogSource.LogError("CreateClonedPlayerModelView - Parent or Prefab is null.");
                return null;
            }
            GameObject instance = Object.Instantiate(prefab, parent.transform);
            instance.name = MenuOverhaulConstants.PlayerModel.MainMenuPlayerModelViewName;
            instance.SetActive(true);
            return instance;
        }

        private static void ConfigurePlayerModelVisuals(GameObject modelInstance)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - modelInstance is null."); return; }

            modelInstance.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -150f, 0f);
            modelInstance.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            Transform cameraTransform = modelInstance.transform.Find(MenuOverhaulConstants.PlayerModel.CameraInventoryPath);
            if (cameraTransform != null)
            {
                UpdateCameraPosition();
                UpdateCameraRotation();

                PrismEffects prismEffects = cameraTransform.GetComponent<PrismEffects>();
                if (prismEffects != null)
                {
                    prismEffects.toneValues = new Vector3(7f, 1.25f, 1.25f);
                    prismEffects.exposureUpperLimit = 0.55f;
                    prismEffects.useExposure = true;
                }
                else { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - PrismEffects component not found on Camera_inventory."); }
            }
            else { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - Camera_inventory not found in player model instance."); }

            ConfigurePreviewRenderQuality(modelInstance);
            LightHelpers.SetupLights(modelInstance);
            UpdatePlayerModelRotation();
        }

        private static void ConfigurePreviewRenderQuality(GameObject modelInstance)
        {
            if (modelInstance == null)
            {
                return;
            }

            CameraImage cameraImage = modelInstance.GetComponentInChildren<CameraImage>(true);
            Transform cameraTransform = modelInstance.transform.Find(MenuOverhaulConstants.PlayerModel.CameraInventoryPath);
            Camera previewCamera = null;
            if (cameraTransform != null)
            {
                previewCamera = cameraTransform.GetComponent<Camera>();
            }

            if (cameraImage == null || previewCamera == null)
            {
                Plugin.LogSource.LogWarning("ConfigurePreviewRenderQuality - CameraImage or Camera_inventory missing on player model preview.");
                return;
            }

            RectTransform previewRect = cameraImage.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                if (!_basePreviewViewportSize.HasValue)
                {
                    Vector2 detectedSize = previewRect.rect.size;
                    if (detectedSize.x <= 0f || detectedSize.y <= 0f)
                    {
                        detectedSize = previewRect.sizeDelta;
                    }

                    if (detectedSize.x > 0f && detectedSize.y > 0f)
                    {
                        _basePreviewViewportSize = detectedSize;
                    }
                }

                if (!_basePreviewViewportScale.HasValue)
                {
                    _basePreviewViewportScale = previewRect.localScale;
                }

                if (_basePreviewViewportSize.HasValue && _basePreviewViewportScale.HasValue)
                {
                    float previewSupersampleFactor = GetPreviewSupersampleFactor();

                    Vector2 baseSize = _basePreviewViewportSize.Value;
                    previewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseSize.x * previewSupersampleFactor);
                    previewRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseSize.y * previewSupersampleFactor);

                    Vector3 baseScale = _basePreviewViewportScale.Value;
                    previewRect.localScale = new Vector3(baseScale.x / previewSupersampleFactor, baseScale.y / previewSupersampleFactor, baseScale.z);
                }
            }

            int qualityAA = QualitySettings.antiAliasing;
            int resolvedAA = qualityAA <= 1
                ? (Settings.EnableHighQualityPlayerPreview.Value ? 8 : 1)
                : qualityAA;
            if (resolvedAA != 1 && resolvedAA != 2 && resolvedAA != 4 && resolvedAA != 8)
            {
                resolvedAA = Settings.EnableHighQualityPlayerPreview.Value ? 8 : 1;
            }

            cameraImage.TextureDepth = 24;
            cameraImage.TextureFormat = RenderTextureFormat.ARGB32;
            cameraImage.RenderTextureReadWrite = RenderTextureReadWrite.Default;
            cameraImage.Antialiasing = resolvedAA;

            previewCamera.allowMSAA = true;

            cameraImage.InitCamera(previewCamera);
        }

        private static void SetupBottomField(GameObject modelInstance, GameObject playerLevelViewPrefab, GameObject playerLevelIconViewPrefab)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("SetupBottomField - modelInstance is null."); return; }

            Transform bottomFieldTransform = modelInstance.transform.Find(MenuOverhaulConstants.PlayerModel.BottomFieldName);
            if (bottomFieldTransform == null)
            {
                Plugin.LogSource.LogError("SetupBottomField - BottomField transform not found in modelInstance.");
                return;
            }

            RectTransform bftRect = bottomFieldTransform.GetComponent<RectTransform>();
            if (bftRect == null)
            {
                Plugin.LogSource.LogError("SetupBottomField - RectTransform not found on BottomField. Cannot configure layout.");
                return;
            }
            ConfigureBottomFieldLayout(bottomFieldTransform, bftRect);
            RemoveDynamicBottomFieldChildren(bottomFieldTransform);

            GameObject levelInfoRow = CreateLevelInfoRow(bottomFieldTransform, playerLevelViewPrefab, playerLevelIconViewPrefab);
            GameObject nicknameTextGo = CreateNicknameRow(bottomFieldTransform);
            GameObject experienceRow = CreateExperienceRow(bottomFieldTransform);

            DisableOriginalNicknameAndKarma(bottomFieldTransform);
            SetBottomFieldRowOrder(levelInfoRow, nicknameTextGo, experienceRow);

            UpdatePlayerStats();
        }

        private static void ConfigureBottomFieldLayout(Transform bottomFieldTransform, RectTransform bftRect)
        {
            bftRect.anchorMin = new Vector2(0, 1);
            bftRect.anchorMax = new Vector2(0, 1);
            bftRect.pivot = new Vector2(0, 1);
            bftRect.sizeDelta = new Vector2(0, 0);

            ContentSizeFitter bftCsf = bottomFieldTransform.GetComponent<ContentSizeFitter>();
            if (bftCsf == null) bftCsf = bottomFieldTransform.gameObject.AddComponent<ContentSizeFitter>();
            bftCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup bftVlg = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
            if (bftVlg != null)
            {
                bftVlg.childAlignment = TextAnchor.UpperRight;
                bftVlg.spacing = 15f;
                bftVlg.padding = new RectOffset(0, 0, 0, 0);
                bftVlg.childForceExpandHeight = false;
                bftVlg.childControlHeight = true;
                bftVlg.childForceExpandWidth = false;
                bftVlg.childControlWidth = true;
            }
            else
            {
                Plugin.LogSource.LogWarning("SetupBottomField - VerticalLayoutGroup component not found on BottomField. Row layout might be incorrect.");
            }

            float compensatedBottomFieldScale = 0.6f * GetPreviewSupersampleFactor();
            bottomFieldTransform.localScale = new Vector3(compensatedBottomFieldScale, compensatedBottomFieldScale, compensatedBottomFieldScale);
            bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);
        }

        private static void RemoveDynamicBottomFieldChildren(Transform bottomFieldTransform)
        {
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.LevelGroup);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.LevelInfoRow);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.NicknameText);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.ExperienceRow);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.SpacerLevelInfoNickname);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.SpacerNicknameExperience);
            DestroyBottomFieldChildIfExists(bottomFieldTransform, MenuOverhaulConstants.BottomFieldUi.Experience);
        }

        private static void DestroyBottomFieldChildIfExists(Transform bottomFieldTransform, string childName)
        {
            Transform child = bottomFieldTransform.Find(childName);
            if (child != null)
            {
                Object.Destroy(child.gameObject);
            }
        }

        private static GameObject CreateLevelInfoRow(Transform bottomFieldTransform, GameObject playerLevelViewPrefab, GameObject playerLevelIconViewPrefab)
        {
            GameObject levelInfoRow = new GameObject(MenuOverhaulConstants.BottomFieldUi.LevelInfoRow);
            levelInfoRow.transform.SetParent(bottomFieldTransform, false);

            RectTransform levelInfoRect = levelInfoRow.AddComponent<RectTransform>();
            levelInfoRect.anchorMin = new Vector2(1, 1);
            levelInfoRect.anchorMax = new Vector2(1, 1);
            levelInfoRect.pivot = new Vector2(1, 1);

            LayoutElement levelInfoRowLe = levelInfoRow.AddComponent<LayoutElement>();
            levelInfoRowLe.minHeight = 56f;
            levelInfoRowLe.preferredHeight = 56f;
            levelInfoRowLe.flexibleHeight = 0f;

            HorizontalLayoutGroup levelInfoHlg = levelInfoRow.AddComponent<HorizontalLayoutGroup>();
            levelInfoHlg.padding = new RectOffset(0, 0, 0, 0);
            levelInfoHlg.childAlignment = TextAnchor.MiddleRight;
            levelInfoHlg.spacing = 0f;
            levelInfoHlg.childForceExpandWidth = false;
            levelInfoHlg.childForceExpandHeight = false;
            levelInfoHlg.childControlWidth = true;
            levelInfoHlg.childControlHeight = true;
            levelInfoHlg.reverseArrangement = true;

            ContentSizeFitter levelInfoCsf = levelInfoRow.AddComponent<ContentSizeFitter>();
            levelInfoCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            levelInfoCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddLevelText(levelInfoRow, playerLevelViewPrefab);
            AddLevelIcon(levelInfoRow, playerLevelIconViewPrefab);

            return levelInfoRow;
        }

        private static void AddLevelText(GameObject levelInfoRow, GameObject playerLevelViewPrefab)
        {
            if (playerLevelViewPrefab == null)
            {
                Plugin.LogSource.LogWarning("SetupBottomField - playerLevelViewPrefab is null. Level text will be missing.");
                return;
            }

            GameObject clonedLevel = Object.Instantiate(playerLevelViewPrefab, levelInfoRow.transform);
            clonedLevel.name = MenuOverhaulConstants.BottomFieldUi.Level;
            clonedLevel.SetActive(true);

            RectTransform levelRect = clonedLevel.GetComponent<RectTransform>();
            if (levelRect != null)
            {
                levelRect.anchorMin = new Vector2(1, 0.5f);
                levelRect.anchorMax = new Vector2(1, 0.5f);
                levelRect.pivot = new Vector2(1, 0.5f);
            }

            TextMeshProUGUI levelTMP = clonedLevel.GetComponent<TextMeshProUGUI>();
            if (levelTMP != null)
            {
                levelTMP.alignment = TextAlignmentOptions.Right;
                levelTMP.fontSize = 54f;
                levelTMP.enableWordWrapping = false;
                levelTMP.margin = Vector4.zero;
            }

            LayoutElement levelTextLe = clonedLevel.GetComponent<LayoutElement>();
            if (levelTextLe == null) levelTextLe = clonedLevel.AddComponent<LayoutElement>();
            levelTextLe.minHeight = 56f;
            levelTextLe.preferredHeight = 56f;
        }

        private static void AddLevelIcon(GameObject levelInfoRow, GameObject playerLevelIconViewPrefab)
        {
            if (playerLevelIconViewPrefab == null)
            {
                Plugin.LogSource.LogWarning("SetupBottomField - playerLevelIconViewPrefab is null. Level Icon will be missing.");
                return;
            }

            GameObject clonedIcon = Object.Instantiate(playerLevelIconViewPrefab, levelInfoRow.transform);
            clonedIcon.name = MenuOverhaulConstants.BottomFieldUi.LevelIcon;
            clonedIcon.SetActive(true);

            RectTransform iconRect = clonedIcon.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(1, 0.5f);
                iconRect.anchorMax = new Vector2(1, 0.5f);
                iconRect.pivot = new Vector2(1, 0.5f);
                iconRect.sizeDelta = new Vector2(56f, 56f);
                iconRect.offsetMax = new Vector2(-5f, iconRect.offsetMax.y);
            }

            LayoutElement iconLe = clonedIcon.GetComponent<LayoutElement>();
            if (iconLe == null) iconLe = clonedIcon.AddComponent<LayoutElement>();
            iconLe.minHeight = 56f;
            iconLe.minWidth = 56f;
            iconLe.preferredHeight = 56f;
            iconLe.preferredWidth = 56f;

            Image iconImage = clonedIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
            }
        }

        private static GameObject CreateNicknameRow(Transform bottomFieldTransform)
        {
            GameObject nicknameTextGo = new GameObject(MenuOverhaulConstants.BottomFieldUi.NicknameText);
            nicknameTextGo.transform.SetParent(bottomFieldTransform, false);

            RectTransform nicknameRect = nicknameTextGo.AddComponent<RectTransform>();
            nicknameRect.anchorMin = new Vector2(1, 1);
            nicknameRect.anchorMax = new Vector2(1, 1);
            nicknameRect.pivot = new Vector2(1, 1);

            LayoutElement nicknameTextGoLe = nicknameTextGo.AddComponent<LayoutElement>();
            nicknameTextGoLe.minHeight = 40f;
            nicknameTextGoLe.preferredHeight = 40f;
            nicknameTextGoLe.flexibleHeight = 0f;

            TextMeshProUGUI nicknameTMP = nicknameTextGo.AddComponent<TextMeshProUGUI>();
            nicknameTMP.fontSize = 36f * 1.6f;
            nicknameTMP.color = Settings.AccentColor.Value;
            nicknameTMP.margin = Vector4.zero;
            nicknameTMP.lineSpacing = 0;
            nicknameTMP.alignment = TextAlignmentOptions.Right;
            nicknameTMP.enableWordWrapping = false;

            GameObject nicknameAndKarmaSourceObject = GameObject.Find(MenuOverhaulConstants.PlayerModel.BottomFieldNicknameAndKarmaSourcePath);
            Transform nicknameAndKarmaSourceOriginal = nicknameAndKarmaSourceObject != null ? nicknameAndKarmaSourceObject.transform : null;
            if (nicknameAndKarmaSourceOriginal != null)
            {
                TextMeshProUGUI originalNicknameTMP = nicknameAndKarmaSourceOriginal.GetComponentInChildren<TextMeshProUGUI>();
                if (originalNicknameTMP != null)
                {
                    if (originalNicknameTMP.font != null)
                    {
                        nicknameTMP.font = originalNicknameTMP.font;
                    }

                    nicknameTMP.fontSize = originalNicknameTMP.fontSize * 1.6f;
                }
            }

            ContentSizeFitter nicknameCsf = nicknameTextGo.AddComponent<ContentSizeFitter>();
            nicknameCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nicknameCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return nicknameTextGo;
        }

        private static GameObject CreateExperienceRow(Transform bottomFieldTransform)
        {
            GameObject experienceRow = new GameObject(MenuOverhaulConstants.BottomFieldUi.ExperienceRow);
            experienceRow.transform.SetParent(bottomFieldTransform, false);

            RectTransform expRect = experienceRow.AddComponent<RectTransform>();
            expRect.anchorMin = new Vector2(1, 1);
            expRect.anchorMax = new Vector2(1, 1);
            expRect.pivot = new Vector2(1, 1);

            LayoutElement expRowLe = experienceRow.AddComponent<LayoutElement>();
            expRowLe.minHeight = 35f;
            expRowLe.preferredHeight = 35f;
            expRowLe.flexibleHeight = 0f;

            HorizontalLayoutGroup expHlg = experienceRow.AddComponent<HorizontalLayoutGroup>();
            expHlg.padding = new RectOffset(0, 0, 0, 0);
            expHlg.childAlignment = TextAnchor.MiddleRight;
            expHlg.spacing = 5f;
            expHlg.childForceExpandWidth = false;
            expHlg.childForceExpandHeight = false;
            expHlg.childControlWidth = true;
            expHlg.childControlHeight = true;

            ContentSizeFitter expCsf = experienceRow.AddComponent<ContentSizeFitter>();
            expCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            expCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject expLabelGo = new GameObject(MenuOverhaulConstants.BottomFieldUi.ExpLabel);
            expLabelGo.transform.SetParent(experienceRow.transform, false);

            TextMeshProUGUI expLabelTMP = expLabelGo.AddComponent<TextMeshProUGUI>();
            expLabelTMP.text = "EXP:";
            expLabelTMP.fontSize = 24f;
            expLabelTMP.color = Color.white;
            expLabelTMP.alignment = TextAlignmentOptions.Right;
            expLabelTMP.margin = Vector4.zero;
            expLabelTMP.enableWordWrapping = false;

            GameObject expValueGo = new GameObject(MenuOverhaulConstants.BottomFieldUi.ExpValue);
            expValueGo.transform.SetParent(experienceRow.transform, false);

            TextMeshProUGUI expValueTMP = expValueGo.AddComponent<TextMeshProUGUI>();
            expValueTMP.fontSize = 24f;
            expValueTMP.color = Settings.AccentColor.Value;
            expValueTMP.alignment = TextAlignmentOptions.Right;
            expValueTMP.margin = Vector4.zero;
            expValueTMP.enableWordWrapping = false;

            GameObject experienceOriginalObject = GameObject.Find(MenuOverhaulConstants.PlayerModel.BottomFieldExperienceSourcePath);
            Transform experienceOriginal = experienceOriginalObject != null ? experienceOriginalObject.transform : null;
            if (experienceOriginal != null)
            {
                TextMeshProUGUI originalExpTMP = experienceOriginal.GetComponentInChildren<TextMeshProUGUI>();
                if (originalExpTMP != null && originalExpTMP.font != null)
                {
                    expLabelTMP.font = originalExpTMP.font;
                    expValueTMP.font = originalExpTMP.font;
                }
            }

            return experienceRow;
        }

        private static void DisableOriginalNicknameAndKarma(Transform bottomFieldTransform)
        {
            Transform originalNicknameAndKarma = bottomFieldTransform.Find(MenuOverhaulConstants.BottomFieldUi.NicknameAndKarma);
            if (originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
            }
        }

        private static void SetBottomFieldRowOrder(GameObject levelInfoRow, GameObject nicknameTextGo, GameObject experienceRow)
        {
            if (levelInfoRow != null)
            {
                levelInfoRow.transform.SetAsFirstSibling();
            }

            if (nicknameTextGo != null)
            {
                nicknameTextGo.transform.SetSiblingIndex(1);
            }

            if (experienceRow != null)
            {
                experienceRow.transform.SetSiblingIndex(2);
            }
        }

        private static void HidePlayerModelExtraElements(GameObject modelInstance)
        {
            PlayerProfileTransformController.HidePlayerModelExtraElements(modelInstance);
        }

        private static void AdjustInnerPlayerModelPosition(GameObject modelInstance)
        {
            PlayerProfileTransformController.AdjustInnerPlayerModelPosition(modelInstance);
        }

        private static async Task RefreshPlayerModel()
        {
            if (ClonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("RefreshPlayerModel - clonedPlayerModelView is null. Cannot refresh.");
                return;
            }

            PlayerModelView playerModelViewScript = ClonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript != null)
            {
                playerModelViewScript.Close();
                if (PatchConstants.BackEndSession?.Profile != null)
                {
                    await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, Settings.EnableDefaultPlayerAnimation.Value);
                    AdjustInnerPlayerModelPosition(ClonedPlayerModelView);
                    UpdateCameraPosition();
                    UpdateCameraRotation();
                    ConfigurePreviewRenderQuality(ClonedPlayerModelView);
                }
                else { Plugin.LogSource.LogWarning("RefreshPlayerModel - BackEndSession.Profile is null. Cannot show player model."); }
            }
            else { Plugin.LogSource.LogWarning("RefreshPlayerModel - PlayerModelView script not found on clonedPlayerModelView."); }
        }

        private static void UpdatePlayerStats()
        {
            PlayerProfileStatsController.UpdatePlayerStats(ClonedPlayerModelView, PlayerLevelPanel.SetLevelIcon);
        }

        public void CleanupBeforeDisable()
        {
            UnsubscribeFromProfileSettingsChanges();
            UnsubscribeFromCharacterLevelUpEvent();
        }
    }
}