using EFT.UI;
using System.Reflection;
using System;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class ButtonHelpers
    {
        private const float ButtonIconScale = 0.8f;
        private const float ButtonYOffset = 60f;
        private const float ButtonXOffset = 250f;
        private static readonly string[] ButtonNames = { "PlayButton", "CharacterButton", "TradeButton", "HideoutButton", "ExitButtonGroup" };

        public static void SetupButtonIcons(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null)
            {
                Plugin.LogSource.LogWarning("SetupButtonIcons - menuScreenInstance is null.");
                return;
            }
            SetButtonIconTransform(menuScreenInstance, "PlayButton", new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale), new Vector3(-48f, 0f, 0f));
            SetButtonIconTransform(menuScreenInstance, "TradeButton", new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
            SetButtonIconTransform(menuScreenInstance, "HideoutButton", new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
            SetButtonIconTransform(menuScreenInstance, "ExitButtonGroup", new Vector3(ButtonIconScale, ButtonIconScale, ButtonIconScale));
        }

        public static void ProcessButtons(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null)
            {
                Plugin.LogSource.LogWarning("ProcessButtons - menuScreenInstance is null.");
                return;
            }
            foreach (var buttonName in ButtonNames)
            {
                GameObject buttonObject = menuScreenInstance.gameObject.transform.Find(buttonName)?.gameObject;
                if (buttonObject != null)
                {
                    ApplyButtonTransform(buttonObject, buttonName);
                    LayoutHelpers.SetIconImages(buttonObject, buttonName);
                    HandleSpecificButtonLogic(menuScreenInstance, buttonObject, buttonName);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"{buttonName} not found in MenuScreen for processing.");
                }
            }
        }

        private static void ApplyButtonTransform(GameObject buttonObject, string buttonName)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Plugin.LogSource.LogWarning($"RectTransform not found on {buttonName}.");
                return;
            }
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);

            int index = Array.IndexOf(ButtonNames, buttonName);
            if (index < 0)
            {
                Plugin.LogSource.LogError($"Button name {buttonName} not found in predefined list.");
                return;
            }
            float yOffset = -index * ButtonYOffset;
            rectTransform.anchoredPosition = new Vector2(ButtonXOffset, yOffset);
        }

        private static async void HandleSpecificButtonLogic(MenuScreen menuScreenInstance, GameObject buttonObject, string buttonName)
        {
            switch (buttonName)
            {
                case "PlayButton":
                    await HandlePlayButtonLogic(menuScreenInstance, buttonObject);
                    break;
                case "ExitButtonGroup":
                    HandleExitButtonGroupLogic(buttonObject);
                    break;
                default:
                    HideButtonBackgroundAndActivateIcon(buttonObject, buttonName);
                    break;
            }
        }

        private static async Task HandlePlayButtonLogic(MenuScreen menuScreenInstance, GameObject buttonObject)
        {
            FieldInfo playButtonField = typeof(MenuScreen).GetField("_playButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (playButtonField != null)
            {
                ModifyButtonTextComponent(playButtonField, menuScreenInstance, fontSize: 36);
            }
            else
            {
                Plugin.LogSource.LogWarning("_playButton field not found in MenuScreen.");
            }

            await Task.Delay(1);

            LayoutHelpers.SetChildActive(buttonObject, "Background", false);
            GameObject sizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
            if (sizeLabel != null)
            {
                LayoutHelpers.SetChildActive(sizeLabel, "IconContainer", true);
            }
            else
            {
                Plugin.LogSource.LogWarning($"SizeLabel not found for {buttonObject.name}.");
            }
        }

        private static void HandleExitButtonGroupLogic(GameObject buttonGroupObject)
        {
            GameObject exitButton = buttonGroupObject.transform.Find("ExitButton")?.gameObject;
            if (exitButton != null)
            {
                LayoutHelpers.SetChildActive(exitButton, "Background", false);
                GameObject sizeLabel = exitButton.transform.Find("SizeLabel")?.gameObject;
                if (sizeLabel != null)
                {
                    GameObject iconContainer = sizeLabel.transform.Find("IconContainer")?.gameObject;
                    if (iconContainer != null)
                    {
                        LayoutHelpers.SetChildActive(iconContainer, "Icon", true);
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("IconContainer not found in ExitButton's SizeLabel.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("SizeLabel not found in ExitButton.");
                }
                LayoutHelpers.SetIconImages(exitButton, "ExitButton");
            }
            else
            {
                Plugin.LogSource.LogWarning("ExitButton not found in ExitButtonGroup.");
            }
        }

        private static void HideButtonBackgroundAndActivateIcon(GameObject buttonObject, string buttonName)
        {
            LayoutHelpers.SetChildActive(buttonObject, "Background", false);
            GameObject sizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
            if (sizeLabel != null)
            {
                GameObject iconContainer = sizeLabel.transform.Find("IconContainer")?.gameObject;
                if (iconContainer != null)
                {
                    LayoutHelpers.SetChildActive(iconContainer, "Icon", true);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"IconContainer not found for {buttonName} in SizeLabel.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"SizeLabel not found for {buttonName}.");
            }
        }

        public static void SetButtonIconTransform(MenuScreen menuScreenInstance, string buttonName, Vector3? localScale = null, Vector3? anchoredPosition = null)
        {
            if (menuScreenInstance == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - menuScreenInstance is null for button {buttonName}.");
                return;
            }
            GameObject button = menuScreenInstance.gameObject.transform.Find(buttonName)?.gameObject;
            if (button == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - Button {buttonName} not found in menuScreenInstance.");
                return;
            }

            Transform iconOwnerTransform = button.transform;
            if (buttonName == "ExitButtonGroup")
            {
                Transform exitButtonTransform = button.transform.Find("ExitButton");
                if (exitButtonTransform == null)
                {
                    Plugin.LogSource.LogWarning($"SetButtonIconTransform - ExitButton not found in {buttonName}.");
                    return;
                }
                iconOwnerTransform = exitButtonTransform;
            }

            Transform iconTransform = iconOwnerTransform.Find("SizeLabel/IconContainer/Icon");
            if (iconTransform == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - Icon not found for {buttonName} at expected path.");
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
                    Plugin.LogSource.LogWarning($"SetButtonIconTransform - Icon for {buttonName} does not have a RectTransform.");
                }
            }
        }

        public static void ModifyButtonTextComponent(FieldInfo buttonFieldInfo, MenuScreen screenInstance, string newText = null, int fontSize = 0)
        {
            if (buttonFieldInfo == null || screenInstance == null)
            {
                Plugin.LogSource.LogError("ModifyButtonTextComponent - buttonFieldInfo or screenInstance is null.");
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
                        Plugin.LogSource.LogWarning("TextMeshProUGUI component not found on DefaultUIButton for text modification.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("DefaultUIButton instance is null from field info.");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error modifying button text for field {buttonFieldInfo.Name}: {ex.Message}");
            }
        }
    }
}