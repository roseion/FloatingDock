using System;
using System.Collections.Generic;

namespace FloatingDock.Models
{
    public enum DockOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// 单个托盘的独立配置
    /// </summary>
    public class DockConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Dock";
        public bool ShowName { get; set; } = false;
        public List<DockItem> Items { get; set; } = new();
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;
        public bool AlwaysOnTop { get; set; } = true;
        public DockOrientation Orientation { get; set; } = DockOrientation.Horizontal;
        public string ThemeId { get; set; } = "classic-dark";
        public double Opacity { get; set; } = 0.85;
        public string BackgroundColor { get; set; } = string.Empty;
        public bool AutoSnap { get; set; } = true;
        public bool ShowLabels { get; set; } = true;
        public double IconSize { get; set; } = 48;
        public string FontFamily { get; set; } = "Segoe UI";
        public double CornerRadius { get; set; } = 20;
    }
}
