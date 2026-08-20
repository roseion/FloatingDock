# 程序化绘制 FloatingDock 应用图标 (256x256 PNG -> 多尺寸 ICO)
Add-Type -AssemblyName System.Drawing.Common

$pngPath = "d:\ai\qianwen\悬浮app\dist\icon.png"
$icoPath = "d:\ai\qianwen\悬浮app\FloatingDock\app.ico"

function Draw-DockIcon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $size / 256.0  # 缩放系数

    # 悬浮软阴影
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60, 0, 0, 0))
    $g.FillEllipse($shadowBrush, (28 * $s), (150 * $s), (200 * $s), (36 * $s))

    # Dock 主体（深色圆角矩形）
    $dockRect = New-Object System.Drawing.RectangleF((16 * $s), (88 * $s), (224 * $s), (72 * $s))
    $dockPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $r = 24 * $s
    $dockPath.AddArc($dockRect.X, $dockRect.Y, $r, $r, 180, 90)
    $dockPath.AddArc($dockRect.Right - $r, $dockRect.Y, $r, $r, 270, 90)
    $dockPath.AddArc($dockRect.Right - $r, $dockRect.Bottom - $r, $r, $r, 0, 90)
    $dockPath.AddArc($dockRect.X, $dockRect.Bottom - $r, $r, $r, 90, 90)
    $dockPath.CloseFigure()

    $dockBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(0, (88 * $s))),
        (New-Object System.Drawing.PointF(0, (160 * $s))),
        [System.Drawing.Color]::FromArgb(255, 52, 56, 74),
        [System.Drawing.Color]::FromArgb(255, 24, 26, 38))
    $g.FillPath($dockBrush, $dockPath)
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(90, 255, 255, 255), (2 * $s))
    $g.DrawPath($borderPen, $dockPath)

    # 顶部光泽
    $glossBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(0, (88 * $s))),
        (New-Object System.Drawing.PointF(0, (118 * $s))),
        [System.Drawing.Color]::FromArgb(70, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($glossBrush, $dockPath)

    # 4 个应用图标（中间两个放大，模拟鱼眼）
    $colors = @(
        [System.Drawing.Color]::FromArgb(255, 74, 158, 255),   # 蓝
        [System.Drawing.Color]::FromArgb(255, 255, 159, 67),   # 橙
        [System.Drawing.Color]::FromArgb(255, 46, 213, 115),   # 绿
        [System.Drawing.Color]::FromArgb(255, 165, 94, 234)    # 紫
    )
    $sizes  = @(34, 44, 44, 34)
    $xs     = @(36, 84, 132, 184)
    for ($i = 0; $i -lt 4; $i++) {
        $iw = $sizes[$i] * $s
        $ix = $xs[$i] * $s
        $iy = (124 * $s) - ($iw / 2) + (0 * $s)
        $iy = (100 * $s) + ((44 * $s - $iw) / 2)
        $rect = New-Object System.Drawing.RectangleF($ix, $iy, $iw, $iw)
        $ip = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $ir = $iw * 0.28
        $ip.AddArc($rect.X, $rect.Y, $ir, $ir, 180, 90)
        $ip.AddArc($rect.Right - $ir, $rect.Y, $ir, $ir, 270, 90)
        $ip.AddArc($rect.Right - $ir, $rect.Bottom - $ir, $ir, $ir, 0, 90)
        $ip.AddArc($rect.X, $rect.Bottom - $ir, $ir, $ir, 90, 90)
        $ip.CloseFigure()
        $ib = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            (New-Object System.Drawing.PointF($rect.X, $rect.Y)),
            (New-Object System.Drawing.PointF($rect.X, $rect.Bottom)),
            [System.Drawing.Color]::FromArgb(255, [Math]::Min(255, $colors[$i].R + 40), [Math]::Min(255, $colors[$i].G + 40), [Math]::Min(255, $colors[$i].B + 40)),
            $colors[$i])
        $g.FillPath($ib, $ip)
    }

    $g.Dispose()
    return $bmp
}

function Get-PngBytes([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $ms.Dispose()
    return $bytes
}

$ErrorActionPreference = 'Stop'
try {
# 生成 256 主图并保存 PNG（供 README 使用）
$big = Draw-DockIcon 256
Write-Host "big type: $($big.GetType().FullName)"
$big.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# 组装 ICO（PNG 压缩条目: 256 + 48 + 32）
$sizes = @(256, 48, 32)
$pngs = New-Object System.Collections.ArrayList
foreach ($sz in $sizes) {
    $b = Draw-DockIcon $sz
    $bytes = Get-PngBytes $b
    Write-Host "size=$sz png bytes=$($bytes.Length)"
    [void]$pngs.Add([byte[]]$bytes)
    $b.Dispose()
}

$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]$sizes.Count) # count
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # width
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # height
    $bw.Write([byte]0)    # palette
    $bw.Write([byte]0)    # reserved
    $bw.Write([uint16]1)  # planes
    $bw.Write([uint16]32) # bpp
    $bw.Write([uint32]$pngs[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write([byte[]]$p) }
$bw.Dispose()
$fs.Dispose()
$big.Dispose()
} catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Write-Host $_.ScriptStackTrace
}

Write-Output "OK: $icoPath ($('{0:N1}' -f ((Get-Item $icoPath).Length / 1KB)) KB)"
