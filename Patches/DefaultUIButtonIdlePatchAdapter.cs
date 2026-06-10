using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Reflection;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class DefaultUIButtonIdlePatchAdapter : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            if (!EftReflectionMap.IsInitialized)
            {
                EftReflectionMap.Initialize();
            }

            var targetMethod = EftReflectionMap.DefaultButtonIdleMethod;
            if (targetMethod == null)
            {
                EftReflectionMap.WarnIfMissing(EftReflectionMap.DefaultButtonIdleMethod, EftReflectionMap.DefaultButtonIdleMethodKey);
            }

            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            MainMenuButtonAnimationService.ApplyIdle(__instance, animated);
        }
    }
}
