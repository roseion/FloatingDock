using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FloatingDock.Models;
using FloatingDock.Services;

namespace FloatingDock
{
    public partial class SettingsWindow : Window
    {
        private readonly DockConfig _config;
        private readonly AppSettings _settings;
        private readonly DockManager _manager;
        private readonly DockWindow _dockWindow;

        public SettingsWindow(DockConfig config, AppSettings settings,
                              DockManager manager, DockWindow dockWindow)
        {
            InitializeComponent();
            _config = config;
            _settings = settings;
            _manager = manager;
            _dockWindow = dockWindow;
            LoadValues();
        }

        private void LoadValues()
        {
            DockNameBox.Text = _config.Name;
            ShowNameCheck.IsChecked = _config.ShowName;
            OpacitySlider.Value = _config.Opacity;
            OpacityValue.Text = $"{(int)(_config.Opacity * 100)}%";
            UpdateColorPreview();

            CornerRadiusSlider.Value = _config.CornerRadius;
            CornerRadiusValue.Text = ((int)_config.CornerRadius).ToString();

            IconSizeSlider.Value = _config.IconSize;
            IconSizeValue.Text = ((int)_config.IconSize).ToString();

            ShowLabelsCheck.IsChecked = _config.ShowLabels;

            // 加载系统字体
            var fonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(f => f).ToList();
            FontComboBox.ItemsSource = fonts;
            FontComboBox.SelectedItem = _config.FontFamily ?? "Segoe UI";

            if (_config.Orientation == DockOrientation.Horizontal)
                RadioHorizontal.IsChecked = true;
            else
                RadioVertical.IsChecked = true;

            AutoSnapCheck.IsChecked = _config.AutoSnap;
            AlwaysOnTopCheck.IsChecked = _config.AlwaysOnTop;
            AutoStartCheck.IsChecked = AutoStartService.IsEnabled();

            // 生成主题卡片
            BuildThemeCards();
        }

        private void BuildThemeCards()
        {
            ThemePanel.Children.Clear();
            foreach (var theme in ThemeService.AllThemes)
            {
                var card = new Border
                {
                    Width = 100, Height = 64,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = theme.Id
                };

                try
                {
                    card.Background = BuildCardBrush(theme);
                }
                catch
                {
                    card.Background = Brushes.Gray;
                }

                if (_config.ThemeId == theme.Id)
                {
                    card.BorderBrush = Brushes.DodgerBlue;
                    card.BorderThickness = new Thickness(2);
                }

                var tb = new TextBlock
                {
                    Text = theme.Name,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = CreateLabelBrush(theme.LabelColor)
                };
                card.Child = tb;

                card.MouseLeftButtonDown += ThemeCard_Click;
                ThemePanel.Children.Add(card);
            }
        }

        /// <summary>
        /// 根据主题材质构建卡片预览画刷（渐变/纯色）
        /// </summary>
        private static System.Windows.Media.Brush BuildCardBrush(FloatingDock.Models.DockTheme theme)
        {
            if (theme.Material != "solid" && theme.GradientStops.Length >= 2)
            {
                var brush = new System.Windows.Media.LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = theme.Material == "dgradient" ? new Point(1, 1) : new Point(0, 1)
                };
                int n = theme.GradientStops.Length;
                for (int i = 0; i < n; i++)
                {
                    var c = (Color)ColorConverter.ConvertFromString(theme.GradientStops[i]);
                    c.A = 255; // 卡片预览不透明，便于看清材质
                    brush.GradientStops.Add(new System.Windows.Media.GradientStop(c, (double)i / (n - 1)));
                }
                return brush;
            }
            var bg = (Color)ColorConverter.ConvertFromString(theme.Background);
            bg.A = 255;
            return new SolidColorBrush(bg);
        }

        private static Brush CreateLabelBrush(string hex)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { return Brushes.White; }
        }

        private void ThemeCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is string themeId)
            {
                _config.ThemeId = themeId;
                _config.BackgroundColor = string.Empty; // 清除自定义颜色，使用主题默认色
                _manager.SaveAll();
                _dockWindow.RefreshAppearance();
                BuildThemeCards();
                UpdateColorPreview();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValue == null) return;
            _config.Opacity = OpacitySlider.Value;
            OpacityValue.Text = $"{(int)(_config.Opacity * 100)}%";
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void RadioHorizontal_Checked(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.Orientation = DockOrientation.Horizontal;
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void RadioVertical_Checked(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.Orientation = DockOrientation.Vertical;
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void AutoSnapCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.AutoSnap = AutoSnapCheck.IsChecked == true;
            _manager.SaveAll();
        }

        private void AlwaysOnTopCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.AlwaysOnTop = AlwaysOnTopCheck.IsChecked == true;
            _dockWindow.Topmost = _config.AlwaysOnTop;
            _manager.SaveAll();
        }

        private void AutoStartCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            _settings.AutoStart = AutoStartCheck.IsChecked == true;
            AutoStartService.SetEnabled(_settings.AutoStart);
            _manager.SaveAll();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DockNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _config.Name = DockNameBox.Text?.Trim() ?? "";
            // 如果名字为空，自动关闭显示
            if (string.IsNullOrWhiteSpace(_config.Name))
                _config.ShowName = false;
            ShowNameCheck.IsChecked = _config.ShowName;
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void ShowNameCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.ShowName = ShowNameCheck.IsChecked == true;
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void CornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CornerRadiusValue == null) return;
            _config.CornerRadius = (int)CornerRadiusSlider.Value;
            CornerRadiusValue.Text = ((int)_config.CornerRadius).ToString();
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void CornerRadiusValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(CornerRadiusValue.Text, out int val))
            {
                val = Math.Clamp(val, 0, 40);
                _config.CornerRadius = val;
                CornerRadiusSlider.Value = val;
                CornerRadiusValue.Text = val.ToString();
                _manager.SaveAll();
                _dockWindow.RefreshAppearance();
            }
        }

        private void IconSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IconSizeValue == null) return;
            _config.IconSize = (int)IconSizeSlider.Value;
            IconSizeValue.Text = ((int)_config.IconSize).ToString();
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void IconSizeValue_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IconSizeValue.Text, out int val))
            {
                val = Math.Clamp(val, 24, 96);
                _config.IconSize = val;
                IconSizeSlider.Value = val;
                IconSizeValue.Text = val.ToString();
                _manager.SaveAll();
                _dockWindow.RefreshAppearance();
            }
        }

        private void ShowLabelsCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_config == null) return;
            _config.ShowLabels = ShowLabelsCheck.IsChecked == true;
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_config == null || FontComboBox.SelectedItem == null) return;
            _config.FontFamily = FontComboBox.SelectedItem?.ToString() ?? "Segoe UI";
            _manager.SaveAll();
            _dockWindow.RefreshAppearance();
        }

        private void UpdateColorPreview()
        {
            try
            {
                string colorHex = !string.IsNullOrEmpty(_config.BackgroundColor)
                    ? _config.BackgroundColor
                    : ThemeService.GetTheme(_config.ThemeId).Background;
                var c = (Color)ColorConverter.ConvertFromString(colorHex);
                ColorPreview.Background = new SolidColorBrush(c);
                ColorHexText.Text = colorHex;
            }
            catch { }
        }

        private void ChooseColor_Click(object sender, RoutedEventArgs e)
        {
            var popup = new Window
            {
                Title = "选择背景颜色",
                Width = 320, Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            var colors = new[]
            {
                "#1E1E2E", "#000000", "#1A1A2E", "#16213E", "#0F3460",
                "#2D2D2D", "#3C3C3C", "#4A4A4A", "#5C5C5C", "#808080",
                "#E0E0E0", "#FFFFFF", "#F5F5F5", "#FFF8E1", "#E8F5E9",
                "#E3F2FD", "#F3E5F5", "#FCE4EC", "#FFF3E0", "#E0F7FA",
                "#D32F2F", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
                "#2196F3", "#00BCD4", "#009688", "#4CAF50", "#FF9800"
            };

            var panel = new WrapPanel { Margin = new Thickness(12) };
            string? selected = null;

            foreach (var hex in colors)
            {
                var swatch = new Border
                {
                    Width = 40, Height = 40,
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(4),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex))
                };
                swatch.BorderBrush = _config.BackgroundColor.Equals(hex, StringComparison.OrdinalIgnoreCase)
                    ? Brushes.DodgerBlue : Brushes.LightGray;
                swatch.BorderThickness = new Thickness(
                    _config.BackgroundColor.Equals(hex, StringComparison.OrdinalIgnoreCase) ? 2 : 1);
                swatch.MouseLeftButtonDown += (s, ev) =>
                {
                    selected = hex;
                    popup.DialogResult = true;
                    popup.Close();
                };
                panel.Children.Add(swatch);
            }

            var scroll = new ScrollViewer { Content = panel };
            popup.Content = scroll;

            if (popup.ShowDialog() == true && selected != null)
            {
                _config.BackgroundColor = selected;
                _manager.SaveAll();
                _dockWindow.RefreshAppearance();
                UpdateColorPreview();
            }
        }
    }
}
