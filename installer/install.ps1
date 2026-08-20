# FloatingDock 安装脚本（由自解压安装包调用）
# 安装到 %LOCALAPPDATA%\Programs\FloatingDock，无需管理员权限
$ErrorActionPreference = 'Stop'
$sourceDir   = $PSScriptRoot
$installDir  = Join-Path $env:LOCALAPPDATA 'Programs\FloatingDock'
$exeName     = 'FloatingDock.exe'
$appVersion  = '1.0.0'

try {
    # 1. 关闭正在运行的实例
    Get-Process -Name 'FloatingDock' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 500

    # 2. 复制程序文件
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Copy-Item -Path (Join-Path $sourceDir $exeName) -Destination $installDir -Force
    $exePath = Join-Path $installDir $exeName

    # 3. 开始菜单快捷方式
    $startMenu = [Environment]::GetFolderPath('Programs')
    $ws = New-Object -ComObject WScript.Shell
    $lnk = $ws.CreateShortcut((Join-Path $startMenu 'FloatingDock.lnk'))
    $lnk.TargetPath = $exePath
    $lnk.WorkingDirectory = $installDir
    $lnk.Description = 'Windows 浮动 Dock 托盘'
    $lnk.Save()

    # 4. 卸载脚本 + 卸载快捷方式
    $uninstallScript = @'
@echo off
taskkill /F /IM FloatingDock.exe >nul 2>&1
timeout /t 1 /nobreak >nul
set "INSTALLDIR=%LOCALAPPDATA%\Programs\FloatingDock"
del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\FloatingDock.lnk" >nul 2>&1
del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Uninstall FloatingDock.lnk" >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingDock" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "FloatingDock" /f >nul 2>&1
rd /s /q "%INSTALLDIR%" >nul 2>&1
echo FloatingDock has been uninstalled. Your settings in %%AppData%%\FloatingDock are kept.
pause
'@
    $uninstallPath = Join-Path $installDir 'uninstall.cmd'
    Set-Content -Path $uninstallPath -Value $uninstallScript -Encoding ASCII

    $ulnk = $ws.CreateShortcut((Join-Path $startMenu 'Uninstall FloatingDock.lnk'))
    $ulnk.TargetPath = $uninstallPath
    $ulnk.Save()

    # 5. 控制面板"程序和功能"卸载条目（当前用户）
    $regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingDock'
    New-Item -Path $regPath -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayName'    -Value 'FloatingDock' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value $appVersion   -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'Publisher'      -Value 'roseion'     -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayIcon'    -Value $exePath      -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'UninstallString'-Value "`"$uninstallPath`"" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'InstallLocation'-Value $installDir   -PropertyType String -Force | Out-Null

    # 6. 启动应用
    Start-Process -FilePath $exePath

    Write-Host ''
    Write-Host 'FloatingDock installed successfully!' -ForegroundColor Green
    Write-Host "Install path: $installDir"
    Start-Sleep -Seconds 2
}
catch {
    Write-Host "Installation failed: $($_.Exception.Message)" -ForegroundColor Red
    Start-Sleep -Seconds 5
    exit 1
}
