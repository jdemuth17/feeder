$adb = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
if (-not (Test-Path $adb)) { Write-Output "ADB_NOT_FOUND: $adb"; exit 2 }

Write-Output 'Devices:'
& $adb devices

Write-Output 'Package path:'
& $adb shell pm path com.companyname.universalfeeder.mobile

Write-Output 'Package info lines:'
& $adb shell dumpsys package com.companyname.universalfeeder.mobile | Select-String -Pattern 'versionName|versionCode|firstInstallTime|lastUpdateTime|installerPackageName' -AllMatches

Write-Output 'Clearing logcat...'
& $adb logcat -c

$out = Join-Path $PWD 'mobile_logcat_check.txt'
if (Test-Path $out) { Remove-Item $out -Force }

Write-Output "Starting logcat -> $out"
$proc = Start-Process -FilePath $adb -ArgumentList 'logcat -v threadtime' -RedirectStandardOutput $out -NoNewWindow -PassThru
Start-Sleep -Seconds 1

Write-Output 'Launching app (monkey)...'
& $adb shell monkey -p com.companyname.universalfeeder.mobile -c android.intent.category.LAUNCHER 1
Start-Sleep -Seconds 6

if ($proc -ne $null) { $proc.Kill() }
Start-Sleep -Milliseconds 200

Write-Output 'Searching logcat for crash indicators...'
Get-Content $out | Select-String -Pattern 'No assemblies|Fast Deploy|Fast Deployment|FATAL EXCEPTION|Fatal signal|SIGABRT|monodroid|AndroidRuntime|Exception|AVG|avast|suspicious|install' -Context 3,3 | ForEach-Object {
    Write-Output '---MATCH---'
    Write-Output $_.Line
    if ($_.Context.PreContext) { $_.Context.PreContext | ForEach-Object { Write-Output "PRE: $_" } }
    if ($_.Context.PostContext) { $_.Context.PostContext | ForEach-Object { Write-Output "POST: $_" } }
}

Write-Output 'Done.'
