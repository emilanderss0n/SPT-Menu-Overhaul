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
        }

        internal static class PlayerModel
        {
            public const string RootPath = "PlayerMVObject";
            public const string CameraInventoryPath = "PlayerMVObject/Camera_inventory";
            public const string MenuPlayerPath = "PlayerMVObject/MenuPlayer";
            public const string BottomFieldName = "BottomField";

            public const string PlayerModelViewPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView";
            public const string PlayerLevelPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level";
            public const string PlayerLevelIconPrefabPath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon";
            public const string BottomFieldNicknameAndKarmaSourcePath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/NicknameAndKarma";
            public const string BottomFieldExperienceSourcePath = "Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/Experience";
        }
    }
}
