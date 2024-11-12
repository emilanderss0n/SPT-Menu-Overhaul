using EFT.UI;
using System.Reflection;
using System;
using TMPro;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal class ButtonHelpers
    {
        public static void SetButtonIconTransform(MenuScreen __instance, string buttonName, Vector3? localScale = null, Vector3? anchoredPosition = null)
        {
            GameObject button = __instance.transform.Find(buttonName)?.gameObject;
            if (button == null)
            {
                Plugin.LogSource.LogWarning($"SetButtonIconTransform - Button {buttonName} not found in MenuScreen.");
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
