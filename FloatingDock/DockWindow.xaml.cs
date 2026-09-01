using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FloatingDock.Controls;
using FloatingDock.Models;
using FloatingDock.Services;
using Microsoft.Win32;

namespace FloatingDock
{
    public partial class DockWindow : Window
    {
        private readonly DockConfig _config;
        private DockTheme _theme;
        private readonly AppSettings _settings;
        private readonly DockManager _manager;
        private readonly List<DockItem> _items = new();
        private int _currentHoverIndex = -1;

        // 窗口拖动
        private Point _dragStartPoint;
        private bool _isDraggingWindow = false;
        private bool _isMoving = false;

        // 拖拽排序
        private bool _isReordering = false;
        private int _reorderSourceIndex = -1;
        private DockItemControl? _reorderControl = null;

        // 多显示器 P/Invoke
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private Rect GetCurrentScreenWorkArea()
        {
            var helper = new WindowInteropHelper(this);
            IntPtr hMonitor = MonitorFromWindow(helper.Handle, 2); // MONITOR_DEFAULTTONEAREST
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                return new Rect(
                    mi.rcWork.Left, mi.rcWork.Top,
                    mi.rcWork.Right - mi.rcWork.Left,
                    mi.rcWork.Bottom - mi.rcWork.Top);
            }
            return SystemParameters.WorkArea;
        }

        #region Shortcut Parsing

        private static string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                string target = shortcut.TargetPath;
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
                return target;
            }
            catch { return string.Empty; }
        }

        #endregion

        public DockWindow(DockConfig config, DockTheme theme, AppSettings settings, DockManager manager)
        {
            InitializeComponent();
            _config = config;
            _theme = theme;
            _settings = settings;
            _manager = manager;

            MouseLeftButtonDown += DockWindow_MouseLeftButtonDown;
            MouseLeftButtonUp += DockWindow_MouseLeftButtonUp;
            MouseMove += DockWindow_MouseMove;
        }

        #region Window Lifecycle

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            ApplyOpacity();
            ApplyOrientation();

            foreach (var item in _config.Items)
            {
                _items.Add(item);
                AddDockItemControl(item);
            }

            UpdateEmptyHint();
            Topmost = _config.AlwaysOnTop;
            MenuToggleTop.Header = _config.AlwaysOnTop ? "取消置顶" : "置顶窗口";
            RestoreWindowPosition();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveState();
        }

        private void RestoreWindowPosition()
        {
            var screen = GetCurrentScreenWorkArea();
            if (_config.WindowX >= 0 && _config.WindowY >= 0)
            {
                Left = Math.Max(screen.Left, Math.Min(_config.WindowX, screen.Right - ActualWidth));
                Top = Math.Max(screen.Top, Math.Min(_config.WindowY, screen.Bottom - ActualHeight));
            }
            else
            {
                Left = screen.Left + (screen.Width - ActualWidth) / 2;
                Top = screen.Top + screen.Height - ActualHeight - 60;
            }
        }

        private void SaveState()
        {
            _config.WindowX = Left;
            _config.WindowY = Top;
            _config.AlwaysOnTop = Topmost;
            _config.Items = _items.ToList();
            _manager.SaveAll();
        }

        #endregion

        #region Theme Application

        private void ApplyTheme()
        {
            try
            {
                double cr = _config.CornerRadius;

                // 阴影预留空间：多层软阴影需要向外扩展，避免被窗口裁剪
                int layers = _theme.ShadowLayers;
                double spread = layers > 0 ? layers * 2.5 + 3 : 0;
                RootGrid.Margin = new Thickness(spread);

                // 多层半透明圆角边框叠加模拟软阴影（兼容透明窗口，不用 DropShadowEffect）
                ShadowLayers.Children.Clear();
                if (layers > 0)
                {
                    Color sc;
                    try { sc = (Color)ColorConverter.ConvertFromString(_theme.ShadowColor); }
                    catch { sc = Colors.Black; }
                    for (int i = 1; i <= layers; i++)
                    {
                        var c = sc;
                        c.A = (byte)Math.Max(10, 36 - i * 7);
                        double m = i * 2.5;
                        ShadowLayers.Children.Add(new Border
                        {
                            Background = new SolidColorBrush(c),
                            CornerRadius = new CornerRadius(cr + i * 2.5),
                            Margin = new Thickness(-m, -m + 1, -m, -m + 2), // 略向下偏移，更自然
                            IsHitTestVisible = false
                        });
                    }
                }

                // 背景材质（含用户透明度）
                DockBorder.Background = BuildBackgroundBrush(_config.Opacity);

                // 圆角: 用户配置优先，0=直角，>0=自定义值
                DockBorder.CornerRadius = new CornerRadius(cr);
                double px = _theme.PaddingX, py = _theme.PaddingY;
                DockBorder.Padding = new Thickness(px, py, px, py);

                if (_theme.BorderThickness > 0 && _theme.BorderColor != "Transparent")
                {
                    try
                    {
                        var bc = (Color)ColorConverter.ConvertFromString(_theme.BorderColor);
                        DockBorder.BorderBrush = new SolidColorBrush(bc);
                        DockBorder.BorderThickness = new Thickness(_theme.BorderThickness);
                    }
                    catch { }
                }
                else
                {
                    DockBorder.BorderBrush = Brushes.Transparent;
                    DockBorder.BorderThickness = new Thickness(0);
                }

                // 顶部光泽高光（玻璃/金属反光感）
                if (_theme.Gloss > 0.01)
                {
                    double g = _theme.Gloss;
                    var gb = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    };
                    gb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(255 * g), 255, 255, 255), 0));
                    gb.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(255 * g * 0.35), 255, 255, 255), 0.2));
                    gb.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5));
                    GlossOverlay.Background = gb;
                    GlossOverlay.CornerRadius = new CornerRadius(Math.Max(0, cr - 1));
                    // 负Margin抵消Padding，让光泽覆盖整个DockBorder，避免padding环带与内容区出现色差矩形
                    GlossOverlay.Margin = new Thickness(1 - px, 1 - py, 1 - px, 1 - py);
                    GlossOverlay.Visibility = Visibility.Visible;
                }
                else GlossOverlay.Visibility = Visibility.Collapsed;

                // 新拟态斜切边框（左上亮/右下暗，同色浮雕感）
                if (_theme.Bevel)
                {
                    var bb = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1)
                    };
                    bb.GradientStops.Add(new GradientStop(Color.FromArgb(230, 255, 255, 255), 0));
                    bb.GradientStops.Add(new GradientStop(Color.FromArgb(60, 255, 255, 255), 0.5));
                    bb.GradientStops.Add(new GradientStop(Color.FromArgb(90, 40, 45, 55), 1));
                    BevelOverlay.BorderBrush = bb;
                    BevelOverlay.BorderThickness = new Thickness(2);
                    BevelOverlay.CornerRadius = new CornerRadius(Math.Max(0, cr - 1));
                    BevelOverlay.Margin = new Thickness(1 - px, 1 - py, 1 - px, 1 - py);
                    BevelOverlay.Visibility = Visibility.Visible;
                }
                else BevelOverlay.Visibility = Visibility.Collapsed;

                // CRT 扫描线纹理（1px 亮线每 3px 重复）
                if (_theme.Scanlines)
                {
                    var texBrush = new DrawingBrush
                    {
                        Viewport = new Rect(0, 0, 1, 3),
                        ViewportUnits = BrushMappingMode.Absolute,
                        TileMode = TileMode.Tile,
                        Drawing = new GeometryDrawing
                        {
                            Brush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                            Geometry = new RectangleGeometry(new Rect(0, 0, 1, 1))
                        }
                    };
                    TextureOverlay.Background = texBrush;
                    TextureOverlay.CornerRadius = new CornerRadius(cr);
                    TextureOverlay.Margin = new Thickness(-px, -py, -px, -py);
                    TextureOverlay.Visibility = Visibility.Visible;
                }
                else TextureOverlay.Visibility = Visibility.Collapsed;

                DropOverlay.CornerRadius = new CornerRadius(cr + spread);
                DropOverlay.Margin = new Thickness(-spread);
                EmptyHint.Foreground = CreateBrush(_theme.LabelColor);

                // 托盘标题/分隔线跟随主题文字色（浅色主题自动用深色字，保证可读性）
                DockNameLabel.Foreground = CreateBrush(_theme.LabelColor);
                try
                {
                    var lc = (Color)ColorConverter.ConvertFromString(_theme.LabelColor);
                    NameSeparator.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Max(48, lc.A * 3 / 10), lc.R, lc.G, lc.B));
                }
                catch { }

                // 图标名称标签同样跟随主题文字色
                foreach (UIElement child in ItemsPanel.Children)
                    if (child is DockItemControl ic) ic.LabelColor = _theme.LabelColor;
            }
            catch { }
        }

        /// <summary>
        /// 根据主题材质构建背景画刷，并按用户透明度缩放 alpha
        /// </summary>
        private Brush BuildBackgroundBrush(double opacity)
        {
            try
            {
                // 用户自定义颜色优先（退化为纯色材质）
                if (!string.IsNullOrEmpty(_config.BackgroundColor))
                {
                    string hex = _config.BackgroundColor;
                    if (hex.Length == 7 && _theme.Background.Length == 9)
                        hex = _theme.Background.Substring(0, 3) + hex.Substring(1);
                    var uc = (Color)ColorConverter.ConvertFromString(hex);
                    uc.A = (byte)(uc.A * opacity);
                    return new SolidColorBrush(uc);
                }

                // 渐变材质
                if (_theme.Material != "solid" && _theme.GradientStops.Length >= 2)
                {
                    var brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = _theme.Material == "dgradient" ? new Point(1, 1) : new Point(0, 1)
                    };
                    int n = _theme.GradientStops.Length;
                    for (int i = 0; i < n; i++)
                    {
                        var c = (Color)ColorConverter.ConvertFromString(_theme.GradientStops[i]);
                        c.A = (byte)(c.A * opacity);
                        brush.GradientStops.Add(new GradientStop(c, (double)i / (n - 1)));
                    }
                    return brush;
                }

                // 纯色材质
                var sc = (Color)ColorConverter.ConvertFromString(_theme.Background);
                sc.A = (byte)(sc.A * opacity);
                return new SolidColorBrush(sc);
            }
            catch
            {
                return new SolidColorBrush(Color.FromArgb((byte)(204 * opacity), 30, 30, 46));
            }
        }

        private void ApplyOpacity()
        {
            // 透明度变化时重建材质画刷（同时作用于渐变各color标）
            DockBorder.Background = BuildBackgroundBrush(_config.Opacity);
        }

        private void ApplyOrientation()
        {
            bool horizontal = _config.Orientation == DockOrientation.Horizontal;
            OrientationPanel.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
            ItemsPanel.Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical;
            DockBorder.MinHeight = 76;
            DockBorder.MinWidth = horizontal ? 0 : 76;

            // 更新已有图标的间距方向
            double spacing = _theme.ItemSpacing;
            foreach (UIElement child in ItemsPanel.Children)
            {
                if (child is FrameworkElement fe)
                    fe.Margin = horizontal
                        ? new Thickness(spacing, 0, spacing, 0)
                        : new Thickness(0, spacing / 2, 0, spacing / 2);
            }

            ApplyNameLabel();
        }

        private void ApplyNameLabel()
        {
            bool show = _config.ShowName && !string.IsNullOrWhiteSpace(_config.Name);
            DockNameLabel.Text = _config.Name ?? "";
            DockNameLabel.FontFamily = new FontFamily(_config.FontFamily ?? "Segoe UI");
            DockNameLabel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            NameSeparator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (_config.Orientation == DockOrientation.Vertical)
            {
                // 竖向：标题水平居中（至少占满图标列宽，避免长名称导致偏左）
                DockNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                DockNameLabel.MinWidth = _config.IconSize + 16;
                DockNameLabel.TextWrapping = TextWrapping.Wrap;
                DockNameLabel.TextAlignment = TextAlignment.Center;
                DockNameLabel.VerticalAlignment = VerticalAlignment.Top;
                DockNameLabel.Margin = new Thickness(0, 0, 0, 4);
                NameSeparator.Width = 40;
                NameSeparator.Height = 1;
                NameSeparator.HorizontalAlignment = HorizontalAlignment.Center;
                NameSeparator.Margin = new Thickness(0, 0, 0, 8);
            }
            else
            {
                // 横向：标题垂直居中
                DockNameLabel.HorizontalAlignment = HorizontalAlignment.Left;
                DockNameLabel.MinWidth = 0;
                DockNameLabel.TextWrapping = TextWrapping.NoWrap;
                DockNameLabel.TextAlignment = TextAlignment.Left;
                DockNameLabel.VerticalAlignment = VerticalAlignment.Center;
                DockNameLabel.Margin = new Thickness(0, 0, 8, 0);
                NameSeparator.Width = 1;
                NameSeparator.Height = 40;
                NameSeparator.HorizontalAlignment = HorizontalAlignment.Left;
                NameSeparator.Margin = new Thickness(0, 0, 8, 0);
            }
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { return Brushes.White; }
        }

        /// <summary>
        /// 重新应用主题和方向（供 SettingsWindow 调用）
        /// </summary>
        public void RefreshAppearance()
        {
            _theme = ThemeService.GetTheme(_config.ThemeId);
            ApplyTheme();
            ApplyOpacity();
            ApplyOrientation();
            ApplyNameLabel();

            // 重新渲染所有图标以应用新的间距和设置
            ItemsPanel.Children.Clear();
            foreach (var item in _items)
                AddDockItemControl(item);
        }

        #endregion

        #region Dock Items Management

        private void AddDockItemControl(DockItem item)
        {
            var icon = IconExtractor.GetFileIcon(item.TargetPath)
                    ?? IconExtractor.GetFileIcon(item.ShortcutPath)
                    ?? IconExtractor.GetDefaultIcon();

            var control = new DockItemControl
            {
                IconSource = icon,
                DisplayName = item.Name,
                ShortcutPath = item.ShortcutPath,
                TargetPath = item.TargetPath,
                VerticalAlignment = VerticalAlignment.Bottom,
                IconSize = _config.IconSize,
                ShowLabel = _config.ShowLabels,
                ItemFontFamily = _config.FontFamily ?? "Segoe UI",
                LabelColor = _theme.LabelColor
            };

            // 根据方向设置间距
            double spacing = _theme.ItemSpacing;
            control.Margin = _config.Orientation == DockOrientation.Horizontal
                ? new Thickness(spacing, 0, spacing, 0)
                : new Thickness(0, spacing / 2, 0, spacing / 2);

            control.ItemRemoved += DockItem_ItemRemoved;
            control.ItemHoverEntered += DockItem_HoverEntered;
            control.ItemHoverLeft += DockItem_HoverLeft;
            control.ItemReorderDragStart += DockItem_ReorderDragStart;
            ItemsPanel.Children.Add(control);
        }

        private void DockItem_ItemRemoved(object? sender, RoutedEventArgs e)
        {
            if (sender is DockItemControl control)
            {
                int index = ItemsPanel.Children.IndexOf(control);
                if (index >= 0 && index < _items.Count)
                {
                    ResetFishEye();
                    _items.RemoveAt(index);
                    ItemsPanel.Children.RemoveAt(index);
                    UpdateEmptyHint();
                    SaveState();
                }
            }
        }

        private void UpdateEmptyHint()
        {
            EmptyHint.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddShortcut(string shortcutPath)
        {
            if (_items.Any(i => i.ShortcutPath.Equals(shortcutPath, StringComparison.OrdinalIgnoreCase)))
                return;
            string targetPath = ResolveShortcutTarget(shortcutPath);
            var item = new DockItem
            {
                Name = Path.GetFileNameWithoutExtension(shortcutPath),
                ShortcutPath = shortcutPath,
                TargetPath = targetPath
            };
            _items.Add(item);
            AddDockItemControl(item);
            UpdateEmptyHint();
            SaveState();
        }

        private void AddExecutable(string exePath)
        {
            if (_items.Any(i => i.TargetPath.Equals(exePath, StringComparison.OrdinalIgnoreCase)))
                return;
            var item = new DockItem
            {
                Name = Path.GetFileNameWithoutExtension(exePath),
                ShortcutPath = exePath,
                TargetPath = exePath
            };
            _items.Add(item);
            AddDockItemControl(item);
            UpdateEmptyHint();
            SaveState();
        }

        private void AddFolder(string folderPath)
        {
            if (_items.Any(i => i.TargetPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
                return;
            var item = new DockItem
            {
                Name = Path.GetFileName(folderPath) ?? folderPath,
                ShortcutPath = folderPath,
                TargetPath = folderPath
            };
            _items.Add(item);
            AddDockItemControl(item);
            UpdateEmptyHint();
            SaveState();
        }

        #endregion

        #region Fish-Eye Effect

        private void DockItem_HoverEntered(object? sender, int index)
        {
            if (_currentHoverIndex == index) return;
            _currentHoverIndex = index;
            ApplyFishEye(index);
        }

        private void DockItem_HoverLeft(object? sender, int index)
        {
            if (_currentHoverIndex != index) return;
            _currentHoverIndex = -1;
            ResetFishEye();
        }

        private void ApplyFishEye(int hoverIndex)
        {
            double neighbor = _theme.NeighborScale;
            for (int i = 0; i < ItemsPanel.Children.Count; i++)
            {
                if (i == hoverIndex) continue;
                if (i == hoverIndex - 1 || i == hoverIndex + 1)
                {
                    if (ItemsPanel.Children[i] is DockItemControl n)
                        n.SetNeighborScale(neighbor);
                }
                else
                {
                    if (ItemsPanel.Children[i] is DockItemControl o)
                        o.SetNeighborScale(1.0);
                }
            }
        }

        private void ResetFishEye()
        {
            for (int i = 0; i < ItemsPanel.Children.Count; i++)
            {
                if (ItemsPanel.Children[i] is DockItemControl c)
                    c.SetNeighborScale(1.0);
            }
        }

        #endregion

        #region Drag & Drop

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropOverlay.Visibility = Visibility.Visible;
            }
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    foreach (string file in files)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".lnk") AddShortcut(file);
                        else if (ext == ".exe") AddExecutable(file);
                        else if (Directory.Exists(file)) AddFolder(file);
                    }
                }
            }
            e.Handled = true;
        }

        #endregion

        #region Drag Reorder

        private void DockItem_ReorderDragStart(object? sender, int index)
        {
            _isReordering = true;
            _reorderSourceIndex = index;
            _reorderControl = sender as DockItemControl;
            _reorderControl?.SetReorderVisual(true);
            // 确保先释放子控件的捕获，再由窗口捕获
            Mouse.Capture(null);
            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isReordering || _reorderControl == null) return;

            // 安全兜底：左键已松开但拖拽状态残留时，立即结束排序（防止图标一直跟随移动）
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                FinalizeReorder();
                return;
            }

            // 确保窗口持有鼠标捕获
            if (Mouse.Captured != this)
                CaptureMouse();

            Point mousePos = e.GetPosition(ItemsPanel);

            // 计算目标索引
            int count = ItemsPanel.Children.Count;
            if (count <= 1) return;

            bool horizontal = _config.Orientation == DockOrientation.Horizontal;
            double pos = horizontal ? mousePos.X : mousePos.Y;
            double extent = horizontal ? ItemsPanel.ActualWidth : ItemsPanel.ActualHeight;

            // 如果 ItemsPanel 尚未布局，回退到估算尺寸
            if (extent < 1)
            {
                double estimatedSize = _config.IconSize + _theme.ItemSpacing * 2 + 20;
                extent = estimatedSize * count;
            }

            double slotSize = extent / count;
            int targetIndex = Math.Clamp((int)(pos / slotSize), 0, count - 1);

            int currentIndex = ItemsPanel.Children.IndexOf(_reorderControl);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                ItemsPanel.Children.RemoveAt(currentIndex);
                var item = _items[currentIndex];
                _items.RemoveAt(currentIndex);

                ItemsPanel.Children.Insert(targetIndex, _reorderControl);
                _items.Insert(targetIndex, item);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_isReordering)
                FinalizeReorder();
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            // 隧道阶段兜底：即使冒泡事件被子元素拦截也能结束排序
            if (_isReordering)
                FinalizeReorder();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            // 捕获意外丢失时清理拖拽状态
            if (_isReordering)
                FinalizeReorder();
        }

        private void FinalizeReorder()
        {
            if (!_isReordering) return;
            _isReordering = false;
            _reorderControl?.SetReorderVisual(false);
            _reorderControl?.ResetReorderState();
            _reorderSourceIndex = -1;
            _reorderControl = null;
            ReleaseMouseCapture();
            SaveState();
        }

        #endregion

        #region Window Dragging & Edge Snapping

        private void DockWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is DockItemControl || FindParent<DockItemControl>(e.OriginalSource as DependencyObject) != null)
                return;
            _dragStartPoint = e.GetPosition(this);
            _isDraggingWindow = true;
            _isMoving = false;
            CaptureMouse();
        }

        private void DockWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingWindow) return;
            Point currentPos = e.GetPosition(this);
            Vector diff = currentPos - _dragStartPoint;
            if (diff.Length > 3)
            {
                _isMoving = true;
                Left += diff.X;
                Top += diff.Y;
            }
        }

        private void DockWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingWindow) return;
            ReleaseMouseCapture();
            _isDraggingWindow = false;
            if (_isMoving)
            {
                if (_config.AutoSnap) SnapToEdge();
                SaveState();
            }
            _isMoving = false;
        }

        private void SnapToEdge()
        {
            var screen = GetCurrentScreenWorkArea();
            const double snap = 30;

            bool atLeft = Left < screen.Left + snap;
            bool atRight = Left + ActualWidth > screen.Right - snap;

            if (atLeft) Left = screen.Left;
            if (atRight) Left = screen.Right - ActualWidth;
            if (Top + ActualHeight > screen.Bottom - snap) Top = screen.Bottom - ActualHeight;
            if (Top < screen.Top + snap) Top = screen.Top;
            Left = Math.Max(screen.Left, Math.Min(Left, screen.Right - ActualWidth));
            Top = Math.Max(screen.Top, Math.Min(Top, screen.Bottom - ActualHeight));

            // 贴靠左右边缘时自动切换为竖向，贴靠上下边缘时恢复横向
            if (atLeft || atRight)
            {
                if (_config.Orientation != DockOrientation.Vertical)
                {
                    _config.Orientation = DockOrientation.Vertical;
                    ApplyOrientation();
                }
            }
            else if (Top <= screen.Top + snap || Top + ActualHeight >= screen.Bottom - snap)
            {
                if (_config.Orientation != DockOrientation.Horizontal)
                {
                    _config.Orientation = DockOrientation.Horizontal;
                    ApplyOrientation();
                }
            }
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        #endregion

        #region Context Menu

        private void MainContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // 判断是否是初始托盘（列表中的第一个）
            bool isPrimary = _settings.Docks.Count > 0 && _settings.Docks[0].Id == _config.Id;
            MenuCloseDock.Visibility = isPrimary ? Visibility.Collapsed : Visibility.Visible;

            MenuRemoveItems.Items.Clear();
            if (_items.Count == 0)
            {
                MenuRemoveItems.IsEnabled = false;
            }
            else
            {
                MenuRemoveItems.IsEnabled = true;
                for (int i = 0; i < _items.Count; i++)
                {
                    var mi = new MenuItem { Header = _items[i].Name, Tag = i };
                    mi.Click += RemoveItem_Click;
                    MenuRemoveItems.Items.Add(mi);
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is int idx && idx >= 0 && idx < _items.Count)
            {
                ResetFishEye();
                _items.RemoveAt(idx);
                ItemsPanel.Children.RemoveAt(idx);
                UpdateEmptyHint();
                SaveState();
            }
        }

        private void MenuToggleTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            MenuToggleTop.Header = Topmost ? "取消置顶" : "置顶窗口";
            SaveState();
        }

        private void MenuAddShortcut_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择快捷方式或程序",
                Filter = "快捷方式 (*.lnk)|*.lnk|可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
            };
            if (dialog.ShowDialog() == true)
            {
                string ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (ext == ".lnk") AddShortcut(dialog.FileName);
                else AddExecutable(dialog.FileName);
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow(_config, _settings, _manager, this);
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }

        private void MenuNewDock_Click(object sender, RoutedEventArgs e)
        {
            var newConfig = new DockConfig { Name = "Dock " + (_settings.Docks.Count + 1) };
            _manager.CreateDock(newConfig);
        }

        private void MenuCloseDock_Click(object sender, RoutedEventArgs e)
        {
            _manager.RemoveDock(_config.Id);
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}
