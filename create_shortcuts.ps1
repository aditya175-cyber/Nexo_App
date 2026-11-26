$wsh = New-Object -ComObject WScript.Shell
$icoLocation = "D:\Nexo\src\WinIsland.UI\Assets\app.ico,0"
$exePath = "D:\Nexo\src\WinIsland.UI\bin\Debug\net8.0-windows10.0.19041.0\Nexo.exe"
$workDir = "D:\Nexo\src\WinIsland.UI\bin\Debug\net8.0-windows10.0.19041.0"

# 1. D:\Nexo\Nexo.lnk
$rootShortcut = "D:\Nexo\Nexo.lnk"
$s2 = $wsh.CreateShortcut($rootShortcut)
$s2.TargetPath = $exePath
$s2.WorkingDirectory = $workDir
$s2.IconLocation = $icoLocation
$s2.Description = "Nexo - Dynamic Island"
$s2.Save()
Write-Host "Created: $rootShortcut"

# 2. Desktop
try {
    $desktop = [Environment]::GetFolderPath("Desktop")
    if (-not [string]::IsNullOrEmpty($desktop)) {
        $desktopShortcut = Join-Path $desktop "Nexo.lnk"
        $s1 = $wsh.CreateShortcut($desktopShortcut)
        $s1.TargetPath = $exePath
        $s1.WorkingDirectory = $workDir
        $s1.IconLocation = $icoLocation
        $s1.Description = "Nexo - Dynamic Island"
        $s1.Save()
        Write-Host "Created: $desktopShortcut"
    }
} catch {
    Write-Warning "Could not write to Desktop, root shortcut is ready."
}
