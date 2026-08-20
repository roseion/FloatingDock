using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FloatingDock.Services
{
    public static class IconExtractor
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string szFileName, int nIconIndex,
            IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        public static ImageSource? GetFileIcon(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || (!File.Exists(filePath) && !Directory.Exists(filePath)))
                return null;

            try
            {
                // 文件夹：直接用 SHGetFileInfo 获取文件夹图标
                if (Directory.Exists(filePath))
                {
                    return GetShellIcon(filePath);
                }

                // 先尝试 ExtractIconEx 获取大图标
                IntPtr[] largeIcons = new IntPtr[1];
                IntPtr[] smallIcons = new IntPtr[1];
                int count = ExtractIconEx(filePath, 0, largeIcons, smallIcons, 1);

                if (count > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    ImageSource source = Imaging.CreateBitmapSourceFromHIcon(
                        largeIcons[0],
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(48, 48));
                    source.Freeze();
                    DestroyIcon(largeIcons[0]);
                    if (smallIcons[0] != IntPtr.Zero)
                        DestroyIcon(smallIcons[0]);
                    return source;
                }

                // 回退: 使用 SHGetFileInfo
                return GetShellIcon(filePath);
            }
            catch
            {
                // 图标提取失败时返回 null
            }

            return null;
        }

        private static ImageSource? GetShellIcon(string path)
        {
            SHFILEINFO shfi = new();
            uint size = (uint)Marshal.SizeOf(typeof(SHFILEINFO));
            IntPtr result = SHGetFileInfo(path, 0, ref shfi, size, SHGFI_ICON | SHGFI_LARGEICON);

            if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                ImageSource source = Imaging.CreateBitmapSourceFromHIcon(
                    shfi.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(48, 48));
                source.Freeze();
                DestroyIcon(shfi.hIcon);
                return source;
            }
            return null;
        }

        public static ImageSource? GetDefaultIcon()
        {
            try
            {
                // 获取系统默认应用程序图标
                string sysPath = Path.Combine(Environment.SystemDirectory, "shell32.dll");
                IntPtr[] largeIcons = new IntPtr[1];
                ExtractIconEx(sysPath, 2, largeIcons, new IntPtr[1], 1);

                if (largeIcons[0] != IntPtr.Zero)
                {
                    ImageSource source = Imaging.CreateBitmapSourceFromHIcon(
                        largeIcons[0],
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(48, 48));
                    source.Freeze();
                    DestroyIcon(largeIcons[0]);
                    return source;
                }
            }
            catch { }
            return null;
        }
    }
}
