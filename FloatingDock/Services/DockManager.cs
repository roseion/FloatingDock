using System.Collections.Generic;
using System.Linq;
using FloatingDock.Models;

namespace FloatingDock.Services
{
    /// <summary>
    /// 多托盘生命周期管理器
    /// </summary>
    public class DockManager
    {
        private readonly AppSettings _settings;
        private readonly Dictionary<string, DockWindow> _windows = new();

        public DockManager(AppSettings settings)
        {
            _settings = settings;
        }

        public AppSettings Settings => _settings;

        /// <summary>
        /// 加载并显示所有托盘
        /// </summary>
        public void LoadAll()
        {
            if (_settings.Docks.Count == 0)
                _settings.Docks.Add(new DockConfig());

            foreach (var dock in _settings.Docks.ToList())
                CreateDockWindow(dock);
        }

        /// <summary>
        /// 创建并显示新托盘
        /// </summary>
        public void CreateDock(DockConfig config)
        {
            _settings.Docks.Add(config);
            CreateDockWindow(config);
            SaveAll();
        }

        /// <summary>
        /// 关闭并移除托盘
        /// </summary>
        public void RemoveDock(string dockId)
        {
            if (_windows.TryGetValue(dockId, out var win))
            {
                win.Close();
                _windows.Remove(dockId);
            }
            _settings.Docks.RemoveAll(d => d.Id == dockId);
            SaveAll();

            if (_windows.Count == 0)
                System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 持久化所有设置
        /// </summary>
        public void SaveAll()
        {
            ConfigService.Save(_settings);
        }

        private void CreateDockWindow(DockConfig config)
        {
            if (_windows.ContainsKey(config.Id)) return;
            var theme = ThemeService.GetTheme(config.ThemeId);
            var win = new DockWindow(config, theme, _settings, this);
            _windows[config.Id] = win;
            win.Show();
        }
    }
}
