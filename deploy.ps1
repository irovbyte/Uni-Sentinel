$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Magenta
Write-Host "  🚀 ULTRA INSTALLER: UNI-SENTINEL 🚀" -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor Magenta

Write-Host "`nКакой стек технологий ты будешь использовать?"
Write-Host "  1) C / C++ (MinGW GCC, Make)" -ForegroundColor Cyan
Write-Host "  2) C# / .NET (Core.AI)" -ForegroundColor Green
Write-Host "  3) Всё и сразу" -ForegroundColor Yellow
$choice = Read-Host "Выбери номер [1-3]"

if ($choice -eq '1' -or $choice -eq '3') {
    Write-Host "`n[+] Установка C/C++ окружения (MinGW-w64, Make)..." -ForegroundColor Cyan
    winget install --id GNU.MinGW-w64 --source winget --accept-package-agreements --accept-source-agreements
    Write-Host "[!] ВАЖНО: Valgrind не работает на Windows. Используй WSL для проверки памяти!" -ForegroundColor Yellow
}
if ($choice -eq '2' -or $choice -eq '3') {
    Write-Host "`n[+] Установка .NET 10 SDK..." -ForegroundColor Green
    winget install --id Microsoft.DotNet.SDK.10 --source winget --accept-package-agreements --accept-source-agreements
}

Write-Host "`n[⬇️] Скачивание ядра Uni-Sentinel..." -ForegroundColor Cyan
$url = "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-win.exe"
$destDir = "$HOME\.uni-sentinel\bin"
if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
Invoke-WebRequest -Uri $url -OutFile "$destDir\uni-sentinel.exe"

$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$destDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$destDir", "User")
    Write-Host "[+] Путь добавлен в переменную среды." -ForegroundColor Green
}

Write-Host "`n✅ УСТАНОВКА ЗАВЕРШЕНА! Перезапусти терминал и введи 'uni-sentinel help'." -ForegroundColor Green
