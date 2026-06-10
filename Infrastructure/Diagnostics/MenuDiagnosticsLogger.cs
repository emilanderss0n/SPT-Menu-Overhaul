using System.Collections.Generic;

namespace MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics
{
    internal enum LogSubsystem
    {
        Lifecycle,
        Layout,
        Buttons,
        Profile,
        Reflection,
        General
    }

    internal static class MenuDiagnosticsLogger
    {
        private static readonly HashSet<string> WarnedKeys = new HashSet<string>();

        public static void Debug(LogSubsystem subsystem, string message)
        {
            if (!IsDebugEnabled(subsystem))
            {
                return;
            }

            Plugin.LogSource?.LogDebug(Format(subsystem, message));
        }

        public static void Info(LogSubsystem subsystem, string message)
        {
            Plugin.LogSource?.LogInfo(Format(subsystem, message));
        }

        public static void Warning(LogSubsystem subsystem, string message)
        {
            Plugin.LogSource?.LogWarning(Format(subsystem, message));
        }

        public static void WarningOnce(LogSubsystem subsystem, string key, string message)
        {
            if (!WarnedKeys.Add(key))
            {
                return;
            }

            Warning(subsystem, message);
        }

        public static void Error(LogSubsystem subsystem, string message)
        {
            Plugin.LogSource?.LogError(Format(subsystem, message));
        }

        private static bool IsDebugEnabled(LogSubsystem subsystem)
        {
            if (MoxoPixel.MenuOverhaul.Utils.Settings.EnableDebugDiagnostics != null
                && !MoxoPixel.MenuOverhaul.Utils.Settings.EnableDebugDiagnostics.Value)
            {
                return false;
            }

            if (MoxoPixel.MenuOverhaul.Utils.Settings.EnableVerboseLifecycleDiagnostics == null)
            {
                return true;
            }

            if (subsystem == LogSubsystem.Lifecycle)
            {
                return MoxoPixel.MenuOverhaul.Utils.Settings.EnableVerboseLifecycleDiagnostics.Value;
            }

            return true;
        }

        private static string Format(LogSubsystem subsystem, string message)
        {
            return $"[{subsystem}] {message}";
        }
    }
}
