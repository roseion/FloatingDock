using System;
using System.IO;
using System.Text.Json;
using FloatingDock.Models;

namespace FloatingDock.Services
{
    public static class ConfigService
    {
        // 配置主路径: %AppData%\FloatingDock\settings.json（安装版不受程序目录权限影响，卸载不丢配置）
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FloatingDock", "settings.json");

        // 旧版路径（exe 旁边），用于自动迁移
        private static readonly string PortableConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static readonly string LegacyConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null) return settings;
                }
            }
            catch { }

            // 迁移 exe 旁边的 settings.json（旧便携版/调试版位置）
            var portable = TryLoadFromFile(PortableConfigPath);
            if (portable != null)
            {
                Save(portable);
                return portable;
            }

            // 尝试迁移更旧的 config.json
            var migrated = TryMigrateLegacyConfig();
            if (migrated != null) return migrated;

            return CreateDefaultSettings();
        }

        private static AppSettings? TryLoadFromFile(string path)
        {
            try
            {
                if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(ConfigPath), StringComparison.OrdinalIgnoreCase)
                    && File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                }
            }
            catch { }
            return null;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        private static AppSettings? TryMigrateLegacyConfig()
        {
            try
            {
                if (File.Exists(LegacyConfigPath))
                {
                    string json = File.ReadAllText(LegacyConfigPath);
                    var legacy = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                    if (legacy != null)
                    {
                        var dock = new DockConfig
                        {
                            Items = legacy.Items,
                            WindowX = legacy.WindowX,
                            WindowY = legacy.WindowY,
                            AlwaysOnTop = legacy.AlwaysOnTop
                        };
                        var settings = new AppSettings();
                        settings.Docks.Add(dock);
                        Save(settings);
                        // 迁移成功后备份旧文件
                        File.Move(LegacyConfigPath, LegacyConfigPath + ".bak", true);
                        return settings;
                    }
                }
            }
            catch { }
            return null;
        }

        private static AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings();
            settings.Docks.Add(new DockConfig());
            return settings;
        }
    }

    // 保留旧模型用于迁移
    internal class AppConfig
    {
        public System.Collections.Generic.List<DockItem> Items { get; set; } = new();
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;
        public bool AlwaysOnTop { get; set; } = true;
    }
}
