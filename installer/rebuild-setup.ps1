# 重建 FloatingDock 安装包（payload.zip + 自解压安装桩）
# 用法: powershell -NoProfile -ExecutionPolicy Bypass -File installer\rebuild-setup.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    # install.ps1 必须为 UTF-8 BOM（Windows PowerShell 5.1 对无 BOM 脚本按 GBK 解码会乱码）
    $p = Join-Path $root 'installer\install.ps1'
    [System.IO.File]::WriteAllText($p, (Get-Content -Raw $p), [System.Text.UTF8Encoding]::new($true))

    Copy-Item $p 'dist\stage\' -Force
    Remove-Item 'dist\payload.zip' -ErrorAction SilentlyContinue
    Compress-Archive -Path 'dist\stage\*' -DestinationPath 'dist\payload.zip' -CompressionLevel Optimal

    $csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    & $csc /nologo /target:winexe /out:'dist\setup-new.exe' /win32icon:'FloatingDock\app.ico' `
        /resource:'dist\payload.zip,payload.zip' /codepage:65001 `
        /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Windows.Forms.dll `
        'installer\stub.cs'
    if ($LASTEXITCODE -ne 0) { throw "csc 编译失败 (exit $LASTEXITCODE)" }

    $locked = Get-Process | Where-Object { $_.Path -like '*FloatingDock-Setup*' }
    if ($locked) {
        Write-Warning '检测到正在运行的安装包进程，请先关闭安装向导窗口再执行本脚本'
        $locked | Select-Object Id, Path | Format-Table | Out-String | Write-Warning
    }

    Move-Item -Force 'dist\setup-new.exe' 'dist\FloatingDock-Setup-1.0.0.exe'
    Get-Item 'dist\FloatingDock-Setup-1.0.0.exe' | Select-Object Name, LastWriteTime, @{N='MB';E={[math]::Round($_.Length/1MB,2)}}
}
finally { Pop-Location }
