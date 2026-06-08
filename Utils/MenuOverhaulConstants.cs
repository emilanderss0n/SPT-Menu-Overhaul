namespace MoxoPixel.MenuOverhaul.Utils
{
    internal static class MenuOverhaulConstants
    {
        internal static class Plugin
        {
            public const string Guid = "com.moxopixel.menuoverhaul";
            public const string Name = "MoxoPixel-MenuOverhaul";
            public const string Version = "1.2.2";
        }

        internal static class Reflection
        {
            public const string MenuScreenShowMethod = "Show";
            public const string DefaultButtonIdleMethod = "method_1";
            public const string DefaultButtonHighlightedMethod = "method_2";
        }

        internal static class MenuScreen
        {
            public const string Name = "MenuScreen";
            public const string ScenePath = "Common UI/Common UI/MenuScreen";
            public const string AlphaWarningField = "_alphaWarningGameObject";
            public const string WarningField = "_warningGameObject";
            public const string PlayButtonField = "_playButton";
        }

        internal static class MenuButtons
        {
            public const string PlayButton = "PlayButton";
            public const string CharacterButton = "CharacterButton";
            public const string TradeButton = "TradeButton";
            public const string HideoutButton = "HideoutButton";
            public const string ExitButtonGroup = "ExitButtonGroup";
            public const string ExitButton = "ExitButton";

            public const string Background = "Background";
            public const string SizeLabel = "SizeLabel";
            public const string IconContainer = "IconContainer";
            public const string Icon = "Icon";

            public const string ExitButtonSizeLabelPath = "ExitButton/SizeLabel";
            public const string IconPath = "SizeLabel/IconContainer/Icon";
        }

        internal static class Environment
        {
            public const string EnvironmentUI = "Environment UI";
            public const string Common = "Common";
            public const string EnvironmentUISceneFactory = "EnvironmentUISceneFactory";
            public const string FactoryLayout = "FactoryLayout";

            public const string Panorama = "panorama";
            public const string CustomPlane = "CustomPlane";
            public const string LampContainer = "LampContainer";
            public const string AlignmentCamera = "AlignmentCamera";
            public const string DecalPlane = "decal_plane";
            public const string DecalPlanePve = "decal_plane_pve";
            public const string GlowCanvas = "Glow Canvas";
            public const string TopGlowPvePath = "Glow Canvas/TopGlowPve";
            public const string FactoryCameraContainer = "FactoryCameraContainer";
            public const string MainMenuCamera = "MainMenuCamera";
            public const string LogotypeBulbLightPath = "LampContainer/Lamp/Lamp/Point light_bulb";
        }

        internal static class PlayerModel
        {
            public const string RootPath = "PlayerMVObject";
            public const string CameraInventoryPath = "PlayerMVObject/Camera_inventory";
            public const string MenuPlayerPath = "PlayerMVObject/MenuPlayer";
            public const string MenuPlayerName = "MenuPlayer";
            public const string BottomFieldName = "BottomField";
            public const string MainMenuPlayerModelViewName = "MainMenuPlayerModelView";
            public const string MainLightPath = "PlayerMVObject/PlayerMVObjectLights/Main Light";
            public const string MainAccentLightPath = "PlayerMVObject/PlayerMVObjectLights/Main Light (2)";
            public const string HairLightPath = "PlayerMVObject/PlayerMVObjectLights/Hair Light";

            public const string PlayerModelViewPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView";
            public const string PlayerLevelPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level";
            public const string PlayerLevelIconPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon";
            public const string BottomFieldNicknameAndKarmaSourcePath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/NicknameAndKarma";
            public const string BottomFieldExperienceSourcePath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/Experience";
        }

        internal static class BottomFieldUi
        {
            public const string LevelGroup = "LevelGroup";
            public const string LevelInfoRow = "LevelInfoRow";
            public const string Level = "Level";
            public const string LevelIcon = "Level Icon";
            public const string NicknameText = "NicknameText";
            public const string NicknameAndKarma = "NicknameAndKarma";
            public const string Experience = "Experience";
            public const string ExperienceRow = "ExperienceRow";
            public const string ExpLabel = "ExpLabel";
            public const string ExpValue = "ExpValue";
            public const string SpacerLevelInfoNickname = "Spacer_LevelInfo_Nickname";
            public const string SpacerNicknameExperience = "Spacer_Nickname_Experience";
        }
    }
}
