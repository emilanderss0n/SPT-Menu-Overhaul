using EFT.UI;
using System.Reflection;
using System;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using MoxoPixel.MenuOverhaul.Infrastructure.Reflection;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class MainMenuButtonRuntime
    {
        private const float ButtonIconScale = 0.8f;
        private const float ButtonYOffset = 60f;
        private const float DefaultButtonXOffset = 250f;
        private static readonly string[] ButtonNames =
        [
            MenuOverhaulConstants.MenuButtons.PlayButton,
            MenuOverhaulConstants.MenuButtons.CharacterButton,
            MenuOverhaulConstants.MenuButtons.TradeButton,
            MenuOverhaulConstants.MenuButtons.HideoutButton,
            MenuOverhaulConstants.MenuButtons.ExitButtonGroup
        ];

        public static void SetupButtonIcons(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "SetupButtonIcons - menuScreenInstance is null.");
                return;
            }
            SetButtonIconTransform(menuScreenInstance, MenuOverhaulConstants.MenuButtons.PlayButton, new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale), new Vector3(-48f, 0f, 0f));
            SetButtonIconTransform(menuScreenInstance, MenuOverhaulConstants.MenuButtons.TradeButton, new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
            SetButtonIconTransform(menuScreenInstance, MenuOverhaulConstants.MenuButtons.HideoutButton, new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
            SetButtonIconTransform(menuScreenInstance, MenuOverhaulConstants.MenuButtons.ExitButtonGroup, new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
        }

        /// <summary>
        /// Re-invokes DefaultUIButtonAnimation.SetNormalState(false) on every button
        /// under the MenuScreen so DefaultUIButtonIdlePatchAdapter can restore icon/label/image
        /// alpha now that MenuScreenVisibilityPolicy.IsMainMenuActive is true.
        /// The game itself runs the initial idle pass before our postfix, so
        /// without this the icons stay invisible until the first hover.
        /// </summary>
        public static void RefreshButtonIdleState(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null) return;

            if (!EftReflectionMap.IsInitialized)
            {
                EftReflectionMap.Initialize();
            }

            var animations = menuScreenInstance.gameObject.GetComponentsInChildren<DefaultUIButtonAnimation>(true);
            if (animations == null || animations.Length == 0) return;

            var method = EftReflectionMap.DefaultButtonIdleMethod;
            EftReflectionMap.WarnIfMissing(method, EftReflectionMap.DefaultButtonIdleMethodKey);
            if (method == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"RefreshButtonIdleState - {MenuOverhaulConstants.Reflection.DefaultButtonIdleMethod} not found on DefaultUIButtonAnimation.");
                return;
            }

            object[] args = new object[] { false };
            foreach (var anim in animations)
            {
                if (anim == null) continue;
                try
                {
                    method.Invoke(anim, args);
                }
                catch (Exception ex)
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"RefreshButtonIdleState - {MenuOverhaulConstants.Reflection.DefaultButtonIdleMethod} invoke failed: {ex.Message}");
                }
            }

            UpdateMenuButtonIconVisibility(menuScreenInstance);
        }

        public static void UpdateMenuButtonGroupPositions(MenuScreen menuScreenInstance = null)
        {
            GameObject menuScreenObject = menuScreenInstance != null
                ? menuScreenInstance.gameObject
                : GameObject.Find(MenuOverhaulConstants.MenuScreen.ScenePath);

            if (menuScreenObject == null)
            {
                return;
            }

            foreach (var buttonName in ButtonNames)
            {
                Transform buttonTransform = menuScreenObject.transform.Find(buttonName);
                GameObject buttonObject = buttonTransform != null ? buttonTransform.gameObject : null;
                if (buttonObject != null)
                {
                    ApplyButtonTransform(buttonObject, buttonName);
                }
            }
        }

        public static void UpdateMenuButtonIconVisibility(MenuScreen menuScreenInstance = null)
        {
            bool showIcons = Settings.EnableMenuButtonIcons.Value;

            GameObject menuScreenObject = menuScreenInstance != null
                ? menuScreenInstance.gameObject
                : GameObject.Find(MenuOverhaulConstants.MenuScreen.ScenePath);

            if (menuScreenObject == null)
            {
                return;
            }

            foreach (var animation in menuScreenObject.GetComponentsInChildren<DefaultUIButtonAnimation>(true))
            {
                if (animation != null && animation.Icon != null)
                {
                    animation.Icon.gameObject.SetActive(showIcons);
                }
            }

            foreach (string buttonName in ButtonNames)
            {
                Transform buttonTransform = menuScreenObject.transform.Find(buttonName);
                GameObject buttonObject = buttonTransform != null ? buttonTransform.gameObject : null;
                if (buttonObject == null)
                {
                    continue;
                }

                Transform sizeLabelTransform = buttonObject.transform.Find(MenuOverhaulConstants.MenuButtons.SizeLabel);
                if (buttonName == MenuOverhaulConstants.MenuButtons.ExitButtonGroup)
                {
                    sizeLabelTransform = buttonObject.transform.Find(MenuOverhaulConstants.MenuButtons.ExitButtonSizeLabelPath);
                }

                if (sizeLabelTransform == null)
                {
                    continue;
                }

                Transform iconContainerTransform = sizeLabelTransform.Find(MenuOverhaulConstants.MenuButtons.IconContainer);
                if (iconContainerTransform != null)
                {
                    iconContainerTransform.gameObject.SetActive(showIcons);
                }
            }
        }

        public static void ProcessButtons(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "ProcessButtons - menuScreenInstance is null.");
                return;
            }
            foreach (var buttonName in ButtonNames)
            {
                Transform buttonTransform = menuScreenInstance.gameObject.transform.Find(buttonName);
                GameObject buttonObject = buttonTransform != null ? buttonTransform.gameObject : null;
                if (buttonObject != null)
                {
                    ApplyButtonTransform(buttonObject, buttonName);
                    MainMenuLayoutRuntime.SetIconImages(buttonObject, buttonName);
                    HandleSpecificButtonLogic(menuScreenInstance, buttonObject, buttonName);
                }
                else
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"{buttonName} not found in MenuScreen for processing.");
                }
            }
        }

        private static void ApplyButtonTransform(GameObject buttonObject, string buttonName)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"RectTransform not found on {buttonName}.");
                return;
            }
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);

            int index = Array.IndexOf(ButtonNames, buttonName);
            if (index < 0)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Buttons, $"Button name {buttonName} not found in predefined list.");
                return;
            }
            float yOffset = -index * ButtonYOffset;
            float xOffset = GetButtonXOffset(buttonName);
            rectTransform.anchoredPosition = new Vector2(xOffset, yOffset);
        }

        private static float GetButtonXOffset(string buttonName)
        {
            return buttonName switch
            {
                MenuOverhaulConstants.MenuButtons.PlayButton => Settings.PositionPlayButtonHorizontal.Value,
                MenuOverhaulConstants.MenuButtons.CharacterButton => Settings.PositionCharacterButtonHorizontal.Value,
                MenuOverhaulConstants.MenuButtons.TradeButton => Settings.PositionTradeButtonHorizontal.Value,
                MenuOverhaulConstants.MenuButtons.HideoutButton => Settings.PositionHideoutButtonHorizontal.Value,
                MenuOverhaulConstants.MenuButtons.ExitButtonGroup => Settings.PositionExitButtonHorizontal.Value,
                _ => DefaultButtonXOffset
            };
        }

        private static async void HandleSpecificButtonLogic(MenuScreen menuScreenInstance, GameObject buttonObject, string buttonName)
        {
            try
            {
                switch (buttonName)
                {
                    case MenuOverhaulConstants.MenuButtons.PlayButton:
                        await HandlePlayButtonLogic(menuScreenInstance, buttonObject);
                        break;
                    case MenuOverhaulConstants.MenuButtons.ExitButtonGroup:
                        HandleExitButtonGroupLogic(buttonObject);
                        break;
                    default:
                        HideButtonBackgroundAndActivateIcon(buttonObject, buttonName);
                        break;
                }
            }
            catch (Exception e)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Buttons, e.ToString());
            }
        }

        private static async Task HandlePlayButtonLogic(MenuScreen menuScreenInstance, GameObject buttonObject)
        {
            if (!EftReflectionMap.IsInitialized)
            {
                EftReflectionMap.Initialize();
            }

            FieldInfo playButtonField = EftReflectionMap.MenuScreenPlayButtonField;
            EftReflectionMap.WarnIfMissing(playButtonField, EftReflectionMap.MenuScreenPlayButtonFieldKey);
            if (playButtonField != null)
            {
                ModifyButtonTextComponent(playButtonField, menuScreenInstance, fontSize: 36);
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "_playButton field not found in MenuScreen.");
            }

            await Task.Delay(1);

            MainMenuLayoutRuntime.SetChildActive(buttonObject, MenuOverhaulConstants.MenuButtons.Background, false);
            Transform sizeLabelTransform = buttonObject.transform.Find(MenuOverhaulConstants.MenuButtons.SizeLabel);
            GameObject sizeLabel = sizeLabelTransform != null ? sizeLabelTransform.gameObject : null;
            if (sizeLabel != null)
            {
                MainMenuLayoutRuntime.SetChildActive(sizeLabel, MenuOverhaulConstants.MenuButtons.IconContainer, Settings.EnableMenuButtonIcons.Value);
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SizeLabel not found for {buttonObject.name}.");
            }
        }

        private static void HandleExitButtonGroupLogic(GameObject buttonGroupObject)
        {
            Transform exitButtonTransform = buttonGroupObject.transform.Find(MenuOverhaulConstants.MenuButtons.ExitButton);
            GameObject exitButton = exitButtonTransform != null ? exitButtonTransform.gameObject : null;
            if (exitButton != null)
            {
                MainMenuLayoutRuntime.SetChildActive(exitButton, MenuOverhaulConstants.MenuButtons.Background, false);
                Transform sizeLabelTransform = exitButton.transform.Find(MenuOverhaulConstants.MenuButtons.SizeLabel);
                GameObject sizeLabel = sizeLabelTransform != null ? sizeLabelTransform.gameObject : null;
                if (sizeLabel != null)
                {
                    Transform iconContainerTransform = sizeLabel.transform.Find(MenuOverhaulConstants.MenuButtons.IconContainer);
                    GameObject iconContainer = iconContainerTransform != null ? iconContainerTransform.gameObject : null;
                    if (iconContainer != null)
                    {
                        MainMenuLayoutRuntime.SetChildActive(iconContainer, MenuOverhaulConstants.MenuButtons.Icon, Settings.EnableMenuButtonIcons.Value);
                    }
                    else
                    {
                        MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "IconContainer not found in ExitButton's SizeLabel.");
                    }
                }
                else
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "SizeLabel not found in ExitButton.");
                }
                MainMenuLayoutRuntime.SetIconImages(exitButton, MenuOverhaulConstants.MenuButtons.ExitButton);
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "ExitButton not found in ExitButtonGroup.");
            }
        }

        private static void HideButtonBackgroundAndActivateIcon(GameObject buttonObject, string buttonName)
        {
            MainMenuLayoutRuntime.SetChildActive(buttonObject, MenuOverhaulConstants.MenuButtons.Background, false);
            Transform sizeLabelTransform = buttonObject.transform.Find(MenuOverhaulConstants.MenuButtons.SizeLabel);
            GameObject sizeLabel = sizeLabelTransform != null ? sizeLabelTransform.gameObject : null;
            if (sizeLabel != null)
            {
                Transform iconContainerTransform = sizeLabel.transform.Find(MenuOverhaulConstants.MenuButtons.IconContainer);
                GameObject iconContainer = iconContainerTransform != null ? iconContainerTransform.gameObject : null;
                if (iconContainer != null)
                {
                    MainMenuLayoutRuntime.SetChildActive(iconContainer, MenuOverhaulConstants.MenuButtons.Icon, Settings.EnableMenuButtonIcons.Value);
                }
                else
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"IconContainer not found for {buttonName} in SizeLabel.");
                }
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SizeLabel not found for {buttonName}.");
            }
        }

        private static void SetButtonIconTransform(MenuScreen menuScreenInstance, string buttonName, Vector3? localScale = null, Vector3? anchoredPosition = null)
        {
            if (menuScreenInstance == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SetButtonIconTransform - menuScreenInstance is null for button {buttonName}.");
                return;
            }
            Transform buttonTransform = menuScreenInstance.gameObject.transform.Find(buttonName);
            GameObject button = buttonTransform != null ? buttonTransform.gameObject : null;
            if (button == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SetButtonIconTransform - Button {buttonName} not found in menuScreenInstance.");
                return;
            }

            Transform iconOwnerTransform = button.transform;
            if (buttonName == MenuOverhaulConstants.MenuButtons.ExitButtonGroup)
            {
                Transform exitButtonTransform = button.transform.Find(MenuOverhaulConstants.MenuButtons.ExitButton);
                if (exitButtonTransform == null)
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SetButtonIconTransform - ExitButton not found in {buttonName}.");
                    return;
                }
                iconOwnerTransform = exitButtonTransform;
            }

            Transform iconTransform = iconOwnerTransform.Find(MenuOverhaulConstants.MenuButtons.IconPath);
            if (iconTransform == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SetButtonIconTransform - Icon not found for {buttonName} at expected path.");
                return;
            }

            if (localScale.HasValue)
            {
                iconTransform.localScale = localScale.Value;
            }

            if (anchoredPosition.HasValue)
            {
                RectTransform iconRectTransform = iconTransform as RectTransform;
                if (iconRectTransform != null)
                {
                    iconRectTransform.anchoredPosition = anchoredPosition.Value;
                }
                else
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, $"SetButtonIconTransform - Icon for {buttonName} does not have a RectTransform.");
                }
            }
        }

        private static void ModifyButtonTextComponent(FieldInfo buttonFieldInfo, MenuScreen screenInstance, string newText = null, int fontSize = 0)
        {
            if (buttonFieldInfo == null || screenInstance == null)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Buttons, "ModifyButtonTextComponent - buttonFieldInfo or screenInstance is null.");
                return;
            }
            try
            {
                DefaultUIButton button = (DefaultUIButton)buttonFieldInfo.GetValue(screenInstance);
                if (button != null)
                {
                    var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        string finalText = string.IsNullOrEmpty(newText) ? textComponent.text : newText;
                        int finalFontSize = fontSize > 0 ? fontSize : (int)textComponent.fontSize;

                        button.SetRawText(finalText, finalFontSize);

                        if (fontSize > 0)
                        {
                            textComponent.fontSize = fontSize;
                        }
                    }
                    else
                    {
                        MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "TextMeshProUGUI component not found on DefaultUIButton for text modification.");
                    }
                }
                else
                {
                    MenuDiagnosticsLogger.Warning(LogSubsystem.Buttons, "DefaultUIButton instance is null from field info.");
                }
            }
            catch (Exception ex)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Buttons, $"Error modifying button text for field {buttonFieldInfo.Name}: {ex.Message}");
            }
        }
    }
}