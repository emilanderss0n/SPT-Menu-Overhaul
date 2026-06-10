using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Reflection;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class DefaultUIButtonHoverPatchAdapter : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            if (!EftReflectionMap.IsInitialized)
            {
                EftReflectionMap.Initialize();
            }

            var targetMethod = EftReflectionMap.DefaultButtonHighlightedMethod;
            if (targetMethod == null)
            {
                EftReflectionMap.WarnIfMissing(EftReflectionMap.DefaultButtonHighlightedMethod, EftReflectionMap.DefaultButtonHighlightedMethodKey);
            }

            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            MainMenuButtonAnimationService.ApplyHover(__instance, animated);
        }
    }
}
