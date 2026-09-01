using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FloatingDock.Controls
{
    public partial class DockItemControl : UserControl
    {
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(DockItemControl));

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(nameof(DisplayName), typeof(string), typeof(DockItemControl));

        public static readonly DependencyProperty ShortcutPathProperty =
            DependencyProperty.Register(nameof(ShortcutPath), typeof(string), typeof(DockItemControl));

        public static readonly DependencyProperty TargetPathProperty =
            DependencyProperty.Register(nameof(TargetPath), typeof(string), typeof(DockItemControl));

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(DockItemControl),
                new PropertyMetadata(48.0));

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(nameof(ShowLabel), typeof(bool), typeof(DockItemControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ItemFontFamilyProperty =
            DependencyProperty.Register(nameof(ItemFontFamily), typeof(string), typeof(DockItemControl),
                new PropertyMetadata("Segoe UI"));

        public static readonly DependencyProperty LabelColorProperty =
            DependencyProperty.Register(nameof(LabelColor), typeof(string), typeof(DockItemControl),
                new PropertyMetadata("#B0FFFFFF", OnLabelColorChanged));

        public ImageSource? IconSource
        {
            get => (ImageSource?)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        public string ShortcutPath
        {
            get => (string)GetValue(ShortcutPathProperty);
            set => SetValue(ShortcutPathProperty, value);
        }

        public string TargetPath
        {
            get => (string)GetValue(TargetPathProperty);
            set => SetValue(TargetPathProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        public string ItemFontFamily
        {
            get => (string)GetValue(ItemFontFamilyProperty);
            set => SetValue(ItemFontFamilyProperty, value);
        }

        public string LabelColor
        {
            get => (string)GetValue(LabelColorProperty);
            set => SetValue(LabelColorProperty, value);
        }

        private static void OnLabelColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DockItemControl c && c.NameLabel != null)
                c.NameLabel.Foreground = CreateLabelBrush(e.NewValue as string);
        }

        private static Brush CreateLabelBrush(string? hex)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex ?? "#B0FFFFFF")); }
            catch { return Brushes.White; }
        }

        public event EventHandler<RoutedEventArgs>? ItemRemoved;
        public event EventHandler<int>? ItemHoverEntered;
        public event EventHandler<int>? ItemHoverLeft;
        public event EventHandler<int>? ItemReorderDragStart;

        private bool _isPressing = false;
        private Point _pressStart;
        private bool _reorderMode = false;

        public bool IsReorderMode => _reorderMode;

        private void OnItemMouseEnter(object sender, MouseEventArgs e)
        {
            int index = GetIndexInParent();
            if (index >= 0)
                ItemHoverEntered?.Invoke(this, index);
        }

        private void OnItemMouseLeave(object sender, MouseEventArgs e)
        {
            int index = GetIndexInParent();
            if (index >= 0)
                ItemHoverLeft?.Invoke(this, index);
        }

        private int GetIndexInParent()
        {
            if (Parent is Panel panel)
                return panel.Children.IndexOf(this);
            return -1;
        }

        /// <summary>
        /// 设置相邻项目的鱼眼缩放效果
        /// </summary>
        public void SetNeighborScale(double scale)
        {
            var anim = new DoubleAnimation(scale, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ItemScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            ItemScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        public DockItemControl()
        {
            InitializeComponent();
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseEnter += OnItemMouseEnter;
            MouseLeave += OnItemMouseLeave;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPressing || _reorderMode) return;
            Point currentPos = e.GetPosition(this);
            Vector diff = currentPos - _pressStart;
            if (diff.Length > 10)
            {
                _reorderMode = true;
                int index = GetIndexInParent();
                if (index >= 0)
                    ItemReorderDragStart?.Invoke(this, index);
            }
        }

        public void SetReorderVisual(bool isDragging)
        {
            Opacity = isDragging ? 0.5 : 1.0;
        }

        public void ResetReorderState()
        {
            _reorderMode = false;
            Opacity = 1.0;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPressing = true;
            _reorderMode = false;
            _pressStart = e.GetPosition(this);
            var scale = new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(80))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            ItemScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
            ItemScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPressing) return;
            _isPressing = false;

            if (_reorderMode)
            {
                _reorderMode = false;
                Opacity = 1.0;
                return; // 拖动排序已完成，不执行启动
            }
            var scaleBack = new DoubleAnimation(1.3, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ItemScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleBack);
            ItemScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleBack);

            // 运行程序
            LaunchShortcut();
        }

        private void LaunchShortcut()
        {
            try
            {
                // 优先启动快捷方式路径，其次目标路径（支持文件和文件夹）
                string path = !string.IsNullOrEmpty(ShortcutPath) && (File.Exists(ShortcutPath) || Directory.Exists(ShortcutPath))
                    ? ShortcutPath
                    : TargetPath;

                if (!string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path)))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法启动程序: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            LaunchShortcut();
        }

        private void MenuShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = !string.IsNullOrEmpty(ShortcutPath) ? ShortcutPath : TargetPath;
                if (!string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path)))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch { }
        }

        private void MenuRemove_Click(object sender, RoutedEventArgs e)
        {
            ItemRemoved?.Invoke(this, e);
        }
    }
}
