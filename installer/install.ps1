# FloatingDock 安装脚本（由自解压安装包调用）
# 支持图形界面选择安装目录（默认 %LOCALAPPDATA%\Programs\FloatingDock），无需管理员权限
# 静默模式: install.ps1 -Silent [-InstallDir "D:\xxx"]
param(
    [switch]$Silent,
    [string]$InstallDir = ''
)

$ErrorActionPreference = 'Stop'
$sourceDir  = $PSScriptRoot
$exeName    = 'FloatingDock.exe'
$appVersion = '1.0.0'
$defaultDir = Join-Path $env:LOCALAPPDATA 'Programs\FloatingDock'
$launchApp  = $true

function Show-InstallDialog {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'FloatingDock 安装'
    $form.ClientSize = New-Object System.Drawing.Size(520, 210)
    $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'FixedDialog'
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.Font = New-Object System.Drawing.Font('Microsoft YaHei UI', 9)

    $lblTitle = New-Object System.Windows.Forms.Label
    $lblTitle.Text = '欢迎安装 FloatingDock 浮动 Dock 托盘'
    $lblTitle.Location = New-Object System.Drawing.Point(16, 14)
    $lblTitle.Size = New-Object System.Drawing.Size(480, 20)
    $lblTitle.Font = New-Object System.Drawing.Font('Microsoft YaHei UI', 10, [System.Drawing.FontStyle]::Bold)

    $lblDir = New-Object System.Windows.Forms.Label
    $lblDir.Text = '安装目录:'
    $lblDir.Location = New-Object System.Drawing.Point(16, 50)
    $lblDir.Size = New-Object System.Drawing.Size(80, 20)

    $txtDir = New-Object System.Windows.Forms.TextBox
    $txtDir.Text = $defaultDir
    $txtDir.Location = New-Object System.Drawing.Point(16, 74)
    $txtDir.Size = New-Object System.Drawing.Size(392, 25)

    $btnBrowse = New-Object System.Windows.Forms.Button
    $btnBrowse.Text = '浏览...'
    $btnBrowse.Location = New-Object System.Drawing.Point(416, 73)
    $btnBrowse.Size = New-Object System.Drawing.Size(88, 26)
    $btnBrowse.Add_Click({
        $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
        $dlg.Description = '选择 FloatingDock 安装目录'
        $dlg.SelectedPath = $txtDir.Text
        if ($dlg.ShowDialog() -eq 'OK') { $txtDir.Text = $dlg.SelectedPath }
    })

    $chkLaunch = New-Object System.Windows.Forms.CheckBox
    $chkLaunch.Text = '安装完成后启动 FloatingDock'
    $chkLaunch.Checked = $true
    $chkLaunch.Location = New-Object System.Drawing.Point(16, 112)
    $chkLaunch.Size = New-Object System.Drawing.Size(300, 22)

    $btnInstall = New-Object System.Windows.Forms.Button
    $btnInstall.Text = '安装'
    $btnInstall.Location = New-Object System.Drawing.Point(324, 156)
    $btnInstall.Size = New-Object System.Drawing.Size(88, 30)
    $form.AcceptButton = $btnInstall

    $btnCancel = New-Object System.Windows.Forms.Button
    $btnCancel.Text = '取消'
    $btnCancel.Location = New-Object System.Drawing.Point(420, 156)
    $btnCancel.Size = New-Object System.Drawing.Size(84, 30)
    $form.CancelButton = $btnCancel

    $form.Controls.AddRange(@($lblTitle, $lblDir, $txtDir, $btnBrowse, $chkLaunch, $btnInstall, $btnCancel))

    $result = $form.ShowDialog()
    $script:chosenDir   = $txtDir.Text.Trim()
    $script:launchApp   = $chkLaunch.Checked
    return ($result -eq 'OK')
}

try {
    # 1. 确定安装目录
    if ($Silent) {
        if ($InstallDir) { $installDir = $InstallDir } else { $installDir = $defaultDir }
    }
    else {
        if (-not (Show-InstallDialog)) {
            Write-Host 'Installation cancelled.'
            exit 0
        }
        $installDir = $chosenDir
        if ([string]::IsNullOrWhiteSpace($installDir)) { $installDir = $defaultDir }
    }

    # 2. 关闭正在运行的实例
    Get-Process -Name 'FloatingDock' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 500

    # 3. 复制程序文件
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Copy-Item -Path (Join-Path $sourceDir $exeName) -Destination $installDir -Force
    $exePath = Join-Path $installDir $exeName

    # 4. 开始菜单快捷方式
    $startMenu = [Environment]::GetFolderPath('Programs')
    $ws = New-Object -ComObject WScript.Shell
    $lnk = $ws.CreateShortcut((Join-Path $startMenu 'FloatingDock.lnk'))
    $lnk.TargetPath = $exePath
    $lnk.WorkingDirectory = $installDir
    $lnk.Description = 'Windows 浮动 Dock 托盘'
    $lnk.Save()

    # 5. 卸载脚本 + 卸载快捷方式（安装目录动态写入）
    $uninstallTemplate = @'
@echo off
taskkill /F /IM FloatingDock.exe >nul 2>&1
timeout /t 1 /nobreak >nul
set "INSTALLDIR=__INSTALLDIR__"
del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\FloatingDock.lnk" >nul 2>&1
del /f /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Uninstall FloatingDock.lnk" >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingDock" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "FloatingDock" /f >nul 2>&1
rd /s /q "%INSTALLDIR%" >nul 2>&1
echo FloatingDock has been uninstalled. Your settings in %%AppData%%\FloatingDock are kept.
pause
'@
    $uninstallPath = Join-Path $installDir 'uninstall.cmd'
    Set-Content -Path $uninstallPath -Value ($uninstallTemplate.Replace('__INSTALLDIR__', $installDir)) -Encoding ASCII

    $ulnk = $ws.CreateShortcut((Join-Path $startMenu 'Uninstall FloatingDock.lnk'))
    $ulnk.TargetPath = $uninstallPath
    $ulnk.Save()

    # 6. 控制面板"程序和功能"卸载条目（当前用户）
    $regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FloatingDock'
    New-Item -Path $regPath -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayName'    -Value 'FloatingDock' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayVersion' -Value $appVersion   -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'Publisher'      -Value 'roseion'     -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'DisplayIcon'    -Value $exePath      -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'UninstallString'-Value "`"$uninstallPath`"" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name 'InstallLocation'-Value $installDir   -PropertyType String -Force | Out-Null

    # 7. 启动应用
    if ($launchApp) {
        Start-Process -FilePath $exePath
    }

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
