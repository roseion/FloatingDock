using System;
using Microsoft.Win32;

namespace FloatingDock.Services
{
    /// <summary>
    /// 开机启动管理（注册表方式）
    /// </summary>
    public static class AutoStartService
    {
        private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "FloatingDock";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                                    ?? AppDomain.CurrentDomain.BaseDirectory + "FloatingDock.exe";
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch { }
        }
    }
}
