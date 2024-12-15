using BepInEx.Configuration;
using System.Collections.Generic;

namespace MoxoPixel.MenuOverhaul.Utils
{
    public class Settings
    {
        private const string GeneralSectionTitle = "1. General";
        private const string AdjustmentsSectionTitle = "2. Adjustments";

        public static ConfigFile Config;

        public static ConfigEntry<bool> EnableBackground;
        public static ConfigEntry<bool> EnableTopGlow;
        public static ConfigEntry<bool> EnableExtraShadows;
        public static ConfigEntry<float> PositionLogotypeHorizontal;
        public static ConfigEntry<float> PositionPlayerModelHorizontal;
        public static ConfigEntry<float> PositionBottomFieldHorizontal;
        public static ConfigEntry<float> PositionBottomFieldVertical;
        public static ConfigEntry<float> scaleBackgroundX;
        public static ConfigEntry<float> scaleBackgroundY;

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile Config)
        {
            ConfigEntries.Add(EnableBackground = Config.Bind(
                GeneralSectionTitle,
                "Enable Background",
                true,
                new ConfigDescription(
                    "Enable or disable the background in the main menu",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(EnableTopGlow = Config.Bind(
                GeneralSectionTitle,
                "Enable Top Glow",
                true,
                new ConfigDescription(
                    "Enable or disable the blue/yellow top glow in the main menu",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(EnableExtraShadows = Config.Bind(
                GeneralSectionTitle,
                "Enable Extra Shadows",
                false,
                new ConfigDescription(
                    "Enable or disable more shadows to make the player in menu more detailed",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PositionLogotypeHorizontal = Config.Bind(
                AdjustmentsSectionTitle,
                "Position Logotype Horizontal",
                -1.9f,
                new ConfigDescription(
                    "Adjust the horizontal position of the logotype",
                    new AcceptableValueRange<float>(-10f, 1f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PositionPlayerModelHorizontal = Config.Bind(
                AdjustmentsSectionTitle,
                "Position Player Model Horizontal",
                400f,
                new ConfigDescription(
                    "Adjust the horizontal position of the player model in the main menu",
                    new AcceptableValueRange<float>(-600f, 600f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PositionBottomFieldHorizontal = Config.Bind(
                AdjustmentsSectionTitle,
                "Position Player Info Horizontal",
                840f,
                new ConfigDescription(
                    "Adjust the horizontal position of the player info text in the main menu",
                    new AcceptableValueRange<float>(0f, 1200f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PositionBottomFieldVertical = Config.Bind(
                AdjustmentsSectionTitle,
                "Position Player Info Vertical",
                0f,
                new ConfigDescription(
                    "Adjust the vertical position of the player info text in the main menu",
                    new AcceptableValueRange<float>(-300f, 300f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(scaleBackgroundX = Config.Bind(
                AdjustmentsSectionTitle,
                "Scale Background Horizontally",
                1.9f,
                new ConfigDescription(
                    "Adjust the horizontal scale of the background image",
                    new AcceptableValueRange<float>(0f, 4f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(scaleBackgroundY = Config.Bind(
                AdjustmentsSectionTitle,
                "Scale Background Vertically",
                0.92f,
                new ConfigDescription(
                    "Adjust the vertical scale of the background image",
                    new AcceptableValueRange<float>(-1f, 3f),
                    new ConfigurationManagerAttributes { })));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }

        public enum EAlignment
        {
            Right = 0,
            Left = 1,
        }
    }
}