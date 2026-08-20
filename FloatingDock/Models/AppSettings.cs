using System.Collections.Generic;

namespace FloatingDock.Models
{
    /// <summary>
    /// 全局应用设置
    /// </summary>
    public class AppSettings
    {
        public bool AutoStart { get; set; } = false;
        public List<DockConfig> Docks { get; set; } = new();
        public string GlobalThemeId { get; set; } = "classic-dark";
    }
}
