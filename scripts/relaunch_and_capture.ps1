param(
    [string]$apkPath
)

$adb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adb)) { Write-Error "ADB not found: $adb"; exit 1 }
if (-not (Test-Path $apkPath)) { Write-Error "APK not found: $apkPath"; exit 2 }

Write-Output "Installing $apkPath"
& $adb install -r $apkPath

Write-Output "Clearing logcat..."
& $adb logcat -c

$out = Join-Path $PWD 'mobile_logcat_relaunch.txt'
if (Test-Path $out) { Remove-Item $out -Force }

Write-Output "Starting background logcat..."
$proc = Start-Process -FilePath $adb -ArgumentList 'logcat -v threadtime' -RedirectStandardOutput $out -NoNewWindow -PassThru
Start-Sleep -Seconds 1

Write-Output 'Launching app via monkey'
& $adb shell monkey -p com.companyname.universalfeeder.mobile -c android.intent.category.LAUNCHER 1
Start-Sleep -Seconds 6

Write-Output 'Stopping logcat and printing output'
$proc.Kill()
Start-Sleep -Milliseconds 200
Get-Content $out -Tail 200
