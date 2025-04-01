using EFT.UI;
using System.Reflection;
using System;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal class ButtonHelpers
    {
        private const float ButtonScale = 0.8f;
        private const float ButtonYOffset = 60f;
        private const float ButtonXOffset = 250f;
        private static readonly string[] buttonNames = { "PlayButton", "CharacterButton", "TradeButton", "HideoutButton", "ExitButtonGroup" };

        public static void SetupButtonIcons(MenuScreen __instance)
        {
            SetButtonIconTransform(__instance, "PlayButton", new Vector3(ButtonScale, ButtonScale, ButtonScale), new Vector3(-48f, 0f, 0f));
            SetButtonIconTransform(__instance, "TradeButton", new Vector3(ButtonScale, ButtonScale, ButtonScale));
            SetButtonIconTransform(__instance, "HideoutButton", new Vector3(ButtonScale, ButtonScale, ButtonScale));
            SetButtonIconTransform(__instance, "ExitButtonGroup", new Vector3(ButtonScale, ButtonScale, ButtonScale));
        }

        public static void ProcessButtons(MenuScreen __instance)
        {
            foreach (var buttonName in buttonNames)
            {
                GameObject buttonObject = __instance.gameObject.transform.Find(buttonName)?.gameObject;
                if (buttonObject != null)
                {
                    SetupButtonTransform(buttonObject, buttonName);
                    LayoutHelpers.SetIconImages(buttonObject, buttonName);
                    HandleSpecialButtons(__instance, buttonObject, buttonName);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"{buttonName} not found in MenuScreen.");
                }
            }
        }

        public static void SetupButtonTransform(GameObject buttonObject, string buttonName)
        {
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);

            int index = Array.IndexOf(buttonNames, buttonName);
            float yOffset = -index * ButtonYOffset;
            rectTransform.anchoredPosition = new Vector2(ButtonXOffset, yOffset);
        }

        private static void HandleSpecialButtons(MenuScreen __instance, GameObject buttonObject, string buttonName)
        {
            if (buttonName == "PlayButton")
            {
                HandlePlayButton(__instance, buttonObject);
            }
            else if (buttonName == "ExitButtonGroup")
            {
                HandleExitButtonGroup(buttonObject);
            }
            else
            {
                HideButtonBackground(buttonObject, buttonName);
            }
        }

        private static async void HandlePlayButton(MenuScreen __instance, GameObject buttonObject)
        {
            FieldInfo playButtonField = typeof(MenuScreen).GetField("_playButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (playButtonField != null)
            {
                ModifyButtonText(playButtonField, __instance, fontSize: 36);
            }
            else
            {
                Plugin.LogSource.LogWarning("_playButton field not found in MenuScreen.");
            }

            await Task.Delay(1);
            buttonObject.transform.Find("Background")?.gameObject.SetActive(false);
            GameObject sizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
            GameObject iconContainer = sizeLabel.transform.Find("IconContainer")?.gameObject;
            if (iconContainer != null)
            {
                iconContainer.SetActive(true);
            }
            else
            {
                Plugin.LogSource.LogWarning($"IconContainer not found for {buttonObject.name}.");
            }
        }

        private static void HandleExitButtonGroup(GameObject buttonObject)
        {
            GameObject exitButton = buttonObject.transform.Find("ExitButton")?.gameObject;
            if (exitButton != null)
            {
                exitButton.transform.Find("Background")?.gameObject.SetActive(false);
                GameObject SizeLabel = exitButton.transform.Find("SizeLabel")?.gameObject;
                GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
                if (icon != null)
                {
                    icon.SetActive(true);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"Icon not found in exitButton.");
                }
                LayoutHelpers.SetIconImages(exitButton, "ExitButton");
            }
            else
            {
                Plugin.LogSource.LogWarning("ExitButton not found in ExitButtonGroup.");
            }
        }

        private static void HideButtonBackground(GameObject buttonObject, string buttonName)
        {
            buttonObject.transform.Find("Background")?.gameObject.SetActive(false);
            GameObject SizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
            GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
            GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
            if (icon != null)
            {
                icon.SetActive(true);
            }
            else
            {
                Plugin.LogSource.LogWarning($"Icon not set active for {buttonName}.");
            }
        }

        public static void SetButtonIconTransform(MenuScreen __instance, string buttonName, Vector3? localScale = null, Vector3? anchoredPosition = null)
        {
            GameObject button = __instance.gameObject.transform.Find(buttonName)?.gameObject;
            if (button == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - Button {buttonName} not found.");
                return;
            }

            GameObject sizeLabel;
            if (buttonName == "ExitButtonGroup")
            {
                GameObject exitButton = button.transform.Find("ExitButton")?.gameObject;
                if (exitButton == null)
                {
                    Plugin.LogSource.LogWarning($"SetButtonIconTransform - ExitButton not found in {buttonName}.");
                    return;
                }
                sizeLabel = exitButton.transform.Find("SizeLabel")?.gameObject;
            }
            else
            {
                sizeLabel = button.transform.Find("SizeLabel")?.gameObject;
            }

            if (sizeLabel == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - SizeLabel not found in {buttonName}.");
                return;
            }

            GameObject iconContainer = sizeLabel.transform.Find("IconContainer")?.gameObject;
            if (iconContainer == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - IconContainer not found in {buttonName}.");
                return;
            }

            GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
            if (icon == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - Icon not found for {buttonName}.");
                return;
            }

            if (localScale.HasValue)
            {
                icon.transform.localScale = localScale.Value;
            }

            if (anchoredPosition.HasValue)
            {
                RectTransform rectTransform = icon.transform as RectTransform;
                if (rectTransform != null && anchoredPosition.HasValue)
                {
                    rectTransform.anchoredPosition = anchoredPosition.Value;
                }
            }
        }

        public static void ModifyButtonText(FieldInfo buttonField, MenuScreen screen, string newText = null, int fontSize = 0)
        {
            try
            {
                if (buttonField != null)
                {
                    DefaultUIButton button = (DefaultUIButton)buttonField.GetValue(screen);
                    if (button != null)
                    {
                        var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            int currentFontSize = (int)textComponent.fontSize;  // Cast to int

                            // Set text and use current font size if new fontSize is not specified
                            button.SetRawText(newText ?? textComponent.text, fontSize > 0 ? fontSize : currentFontSize);

                            // Override the font size directly on TextMeshProUGUI, if specified
                            if (fontSize > 0)
                            {
                                textComponent.fontSize = fontSize;
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("TextMeshProUGUI component not found on DefaultUIButton.");
                        }
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("DefaultUIButton component not found on button.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("Button field is null.");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error modifying button text: {ex.Message}");
            }
        }
    }
}