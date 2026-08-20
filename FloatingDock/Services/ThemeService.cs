using System.Collections.Generic;
using FloatingDock.Models;

namespace FloatingDock.Services
{
    /// <summary>
    /// 10 套内置材质主题 —— 每套主题拥有独立的材质表现（渐变/光泽/凹凸/阴影/纹理），
    /// 而非简单的颜色替换
    /// </summary>
    public static class ThemeService
    {
        private static readonly List<DockTheme> _themes = new()
        {
            // 1. Classic Dark - 哑光深空（柔和哑光面 + 微光泽 + 软阴影）
            new DockTheme
            {
                Id = "classic-dark",
                Name = "Classic Dark",
                Description = "哑光深空，柔和光泽",
                Background = "#E623232F",
                Material = "vgradient",
                GradientStops = new[] { "#E62E2E3C", "#E61B1B24" },
                Gloss = 0.10,
                ShadowLayers = 2, ShadowColor = "#FF000000",
                CornerRadius = 20,
                PaddingX = 12, PaddingY = 8,
                IconSize = 48, ItemSpacing = 6,
                LabelColor = "#B0FFFFFF", FontSize = 10,
                HoverScale = 1.3, NeighborScale = 1.1,
                ShadowType = "none"
            },
            // 2. Glassmorphism - 磨砂玻璃（斜向折射渐变 + 强顶部光泽 + 亮边）
            new DockTheme
            {
                Id = "glassmorphism",
                Name = "Glassmorphism",
                Description = "磨砂玻璃，光线折射",
                Background = "#40FFFFFF",
                Material = "dgradient",
                GradientStops = new[] { "#5AFFFFFF", "#20FFFFFF", "#38FFFFFF" },
                Gloss = 0.35,
                BorderColor = "#60FFFFFF",
                BorderThickness = 1,
                ShadowLayers = 3, ShadowColor = "#FF000000",
                CornerRadius = 24,
                PaddingX = 14, PaddingY = 10,
                IconSize = 48, ItemSpacing = 8,
                LabelColor = "#F0FFFFFF", FontSize = 10,
                HoverScale = 1.25, NeighborScale = 1.08,
                ShadowType = "none"
            },
            // 3. Neumorphism - 新拟态（同色系凹凸浮雕 + 斜切亮暗边框）
            new DockTheme
            {
                Id = "neumorphism",
                Name = "Neumorphism",
                Description = "柔和凹凸，同色浮雕",
                Background = "#FFE3E5EA",
                Material = "dgradient",
                GradientStops = new[] { "#FFEFF1F5", "#FFD8DBE1" },
                Gloss = 0,
                Bevel = true,
                ShadowLayers = 3, ShadowColor = "#FF8F96A3",
                CornerRadius = 16,
                PaddingX = 14, PaddingY = 10,
                IconSize = 44, ItemSpacing = 10,
                LabelColor = "#A04A5058", FontSize = 10, FontFamily = "Segoe UI",
                HoverScale = 1.15, NeighborScale = 1.05, AnimationDurationMs = 150,
                ShadowType = "none"
            },
            // 4. Fluent - 亚克力（深色亚克力层叠 + 顶部微光 + 发丝边框）
            new DockTheme
            {
                Id = "fluent",
                Name = "Fluent",
                Description = "亚克力层叠，系统原生感",
                Background = "#E0202020",
                Material = "vgradient",
                GradientStops = new[] { "#E831313B", "#E8141419" },
                Gloss = 0.14,
                BorderColor = "#26FFFFFF",
                BorderThickness = 1,
                ShadowLayers = 2, ShadowColor = "#FF000000",
                CornerRadius = 8,
                PaddingX = 10, PaddingY = 6,
                IconSize = 40, ItemSpacing = 4,
                LabelColor = "#C0FFFFFF", FontSize = 9,
                HoverScale = 1.2, NeighborScale = 1.05,
                ShadowType = "none"
            },
            // 5. Material 3 - 色块悬浮（纯色表面 + 4层真实海拔阴影）
            new DockTheme
            {
                Id = "material",
                Name = "Material 3",
                Description = "海拔阴影，色块悬浮",
                Background = "#FF2D2D33",
                Material = "solid",
                Gloss = 0,
                ShadowLayers = 4, ShadowColor = "#FF000000",
                CornerRadius = 16,
                PaddingX = 12, PaddingY = 8,
                IconSize = 44, ItemSpacing = 6,
                LabelColor = "#C0FFFFFF", FontSize = 10,
                HoverScale = 1.2, NeighborScale = 1.06,
                ShadowType = "manual"
            },
            // 6. Minimal Light - 云纸（近白纸面 + 极轻光泽 + 柔和浮起阴影）
            new DockTheme
            {
                Id = "minimal-light",
                Name = "Minimal Light",
                Description = "云纸白面，柔和浮起",
                Background = "#F7FAFAFA",
                Material = "vgradient",
                GradientStops = new[] { "#FCFFFFFF", "#F2F4F5F7" },
                Gloss = 0.18,
                BorderColor = "#14000000",
                BorderThickness = 1,
                ShadowLayers = 3, ShadowColor = "#FF6A7280",
                CornerRadius = 20,
                PaddingX = 16, PaddingY = 10,
                IconSize = 44, ItemSpacing = 10,
                LabelColor = "#90333333", FontSize = 10,
                HoverScale = 1.2, NeighborScale = 1.06,
                ShadowType = "none"
            },
            // 7. Brutalist - 粗野主义（纯黑硬面 + 粗白边框 + 直角无修饰）
            new DockTheme
            {
                Id = "brutalist",
                Name = "Brutalist",
                Description = "纯黑硬面，高对比原始感",
                Background = "#FF0A0A0A",
                Material = "solid",
                Gloss = 0,
                BorderColor = "#FFFFFFFF",
                BorderThickness = 3,
                ShadowLayers = 0,
                CornerRadius = 0,
                PaddingX = 10, PaddingY = 8,
                IconSize = 40, ItemSpacing = 8,
                LabelColor = "#FFFFFFFF", FontSize = 11, FontFamily = "Consolas",
                HoverScale = 1.1, NeighborScale = 1.0, AnimationDurationMs = 0,
                ShadowType = "none"
            },
            // 8. Retro Pixel - CRT 显像管（深蓝底 + 扫描线纹理 + 像素边框）
            new DockTheme
            {
                Id = "retro-pixel",
                Name = "Retro Pixel",
                Description = "CRT 扫描线，8-bit 怀旧",
                Background = "#FF1A1C2C",
                Material = "vgradient",
                GradientStops = new[] { "#FF232640", "#FF131423" },
                Gloss = 0.06,
                Scanlines = true,
                BorderColor = "#FF5D275D",
                BorderThickness = 2,
                ShadowLayers = 0,
                CornerRadius = 2,
                PaddingX = 10, PaddingY = 8,
                IconSize = 36, ItemSpacing = 6,
                LabelColor = "#FFB13E53", FontSize = 9, FontFamily = "Consolas",
                HoverScale = 1.15, NeighborScale = 1.0, AnimationDurationMs = 80,
                ShadowType = "none"
            },
            // 9. Aurora - 极光（四色斜向流光 + 发光边框 + 紫晕阴影）
            new DockTheme
            {
                Id = "aurora",
                Name = "Aurora",
                Description = "四色流光，极光发光",
                Background = "#DD1A0A2E",
                Material = "dgradient",
                GradientStops = new[] { "#E0301A5E", "#CC134E8E", "#C00A8E86", "#D65B2E8E" },
                Gloss = 0.20,
                BorderColor = "#589B59FF",
                BorderThickness = 1,
                ShadowLayers = 2, ShadowColor = "#FF7A3AE0",
                CornerRadius = 22,
                PaddingX = 14, PaddingY = 10,
                IconSize = 56, ItemSpacing = 8,
                LabelColor = "#D8E0D0FF", FontSize = 10,
                HoverScale = 1.35, NeighborScale = 1.12,
                ShadowType = "none"
            },
            // 10. macOS Dock - 金属玻璃（三段金属渐变 + 顶部玻璃反光 + 深阴影）
            new DockTheme
            {
                Id = "macos-dock",
                Name = "macOS Dock",
                Description = "金属玻璃，顶部反光",
                Background = "#CC000000",
                Material = "vgradient",
                GradientStops = new[] { "#D94A4A52", "#CC232329", "#E6101014" },
                Gloss = 0.30,
                BorderColor = "#30FFFFFF",
                BorderThickness = 1,
                ShadowLayers = 3, ShadowColor = "#FF000000",
                CornerRadius = 18,
                PaddingX = 12, PaddingY = 6,
                IconSize = 56, ItemSpacing = 4,
                LabelColor = "#D0FFFFFF", FontSize = 9,
                HoverScale = 1.4, NeighborScale = 1.2, AnimationDurationMs = 250,
                ShadowType = "none"
            }
        };

        public static IReadOnlyList<DockTheme> AllThemes => _themes;

        public static DockTheme GetTheme(string themeId)
        {
            foreach (var theme in _themes)
            {
                if (theme.Id == themeId) return theme;
            }
            return _themes[0]; // 默认 Classic Dark
        }
    }
}
