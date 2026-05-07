$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "`n======================================" -ForegroundColor DarkMagenta
Write-Host "  🚀 UNI-SENTINEL AUTO-INSTALLER 🚀   " -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor DarkMagenta

Write-Host "`nКакой стек технологий защищаем, Shadow Monarch?"
Write-Host "  1) C / C++ (MinGW-w64 GCC, Clang)" -ForegroundColor Cyan
Write-Host "  2) C# / .NET (.NET 10 SDK)" -ForegroundColor Green
Write-Host "  3) Всё и сразу (Titan Mode)" -ForegroundColor Yellow

$host.UI.RawUI.FlushInputBuffer()
$choice = Read-Host "Выбери номер [1-3]"

if ($choice -eq '1' -or $choice -eq '3') {
    Write-Host "`n[+] Ставим C/C++ окружение..." -ForegroundColor Cyan
    winget install --id GNU.MinGW-w64 -e --source winget --accept-package-agreements --accept-source-agreements
    winget install --id LLVM.LLVM -e --source winget --accept-package-agreements --accept-source-agreements
    Write-Host "[!] На Windows память (Valgrind) не чекается. Юзай WSL для тестов памяти!" -ForegroundColor DarkYellow
}
if ($choice -eq '2' -or $choice -eq '3') {
    Write-Host "`n[+] Ставим .NET 10 SDK..." -ForegroundColor Green
    winget install --id Microsoft.DotNet.SDK.10 -e --source winget --accept-package-agreements --accept-source-agreements
}

$destDir = "$HOME\.uni-sentinel\bin"
if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }

Write-Host "`n[⬇️] Загрузка ядра Sentinel..." -ForegroundColor Cyan
$url = "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-win.exe"
$exePath = "$destDir\uni-sentinel.exe"

Invoke-WebRequest -Uri $url -OutFile $exePath
Write-Host "[OK] Ядро успешно загружено." -ForegroundColor Green

Write-Host "[+] Интеграция в систему (PATH)..." -ForegroundColor Cyan
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($userPath -notlike "*$destDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$userPath;$destDir", "User")
    Write-Host "[OK] Путь добавлен в реестр." -ForegroundColor Green
}

$env:Path = "$env:Path;$destDir"

Write-Host "`n✅ УСТАНОВКА ЗАВЕРШЕНА ИДЕАЛЬНО!" -ForegroundColor Green
Write-Host "Система готова к бою. Введи: " -NoNewline
Write-Host "uni-sentinel help" -ForegroundColor Magenta
Write-Host ""
