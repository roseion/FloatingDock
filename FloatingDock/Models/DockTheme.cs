namespace FloatingDock.Models
{
    /// <summary>
    /// 主题定义数据模型（含材质系统）
    /// </summary>
    public class DockTheme
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 背景与边框
        public string Background { get; set; } = "#CC1E1E2E";
        public string BorderColor { get; set; } = "Transparent";
        public double BorderThickness { get; set; } = 0;
        public double CornerRadius { get; set; } = 20;

        // 材质系统
        // Material: solid=纯色, vgradient=纵向渐变, dgradient=斜向渐变
        public string Material { get; set; } = "solid";
        // 渐变色标（含alpha的hex，2-4个），空则用Background
        public string[] GradientStops { get; set; } = new string[0];
        // 顶部光泽高光强度 0~1（0=关闭）
        public double Gloss { get; set; } = 0;
        // 多层半透明边框模拟软阴影层数（0=关闭）
        public int ShadowLayers { get; set; } = 0;
        public string ShadowColor { get; set; } = "#FF000000";
        // 新拟态凹凸斜切边框（左上亮/右下暗）
        public bool Bevel { get; set; } = false;
        // CRT扫描线纹理
        public bool Scanlines { get; set; } = false;

        // 间距与尺寸
        public double PaddingX { get; set; } = 12;
        public double PaddingY { get; set; } = 8;
        public double IconSize { get; set; } = 48;
        public double ItemSpacing { get; set; } = 6;

        // 文字
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 10;
        public string LabelColor { get; set; } = "#B0FFFFFF";

        // 动画
        public double HoverScale { get; set; } = 1.3;
        public double AnimationDurationMs { get; set; } = 200;
        public double NeighborScale { get; set; } = 1.1;

        // 阴影 (none=无, manual=手动边框模拟)
        public string ShadowType { get; set; } = "none";
    }
}
