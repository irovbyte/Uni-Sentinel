$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Invoke-WithRetry {
    param([scriptblock]$Task, [int]$Retries = 3, [int]$Delay = 3)
    $attempt = 0
    while ($attempt -lt $Retries) {
        try {
            & $Task
            return $true
        } catch {
            $attempt++
            Write-Host "[!] Ошибка сети/Таймаут. Попытка $attempt из $Retries..." -ForegroundColor DarkYellow
            Start-Sleep -Seconds $Delay
        }
    }
    throw "Критический сбой: сервер не ответил после $Retries попыток."
}

Write-Host "`n======================================" -ForegroundColor DarkMagenta
Write-Host "  🚀 UNI-SENTINEL AUTO-INSTALLER 🚀   " -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor DarkMagenta

Write-Host "`nКакой стек технологий защищаем, Shadow Monarch?"
Write-Host "  1) C / C++ (MinGW-w64 GCC, Clang)" -ForegroundColor Cyan
Write-Host "  2) C# / .NET (.NET 10 SDK)" -ForegroundColor Green
Write-Host "  3) Всё и сразу (Titan Mode)" -ForegroundColor Yellow

$host.UI.RawUI.FlushInputBuffer()
$choice = Read-Host "Выбери номер [1-3]"

if ($choice -match '1|3') {
    Write-Host "`n[+] Проверка C/C++ окружения..." -ForegroundColor Cyan
    
    if (!(Get-Command gcc -ErrorAction SilentlyContinue)) {
        Write-Host "Скачивание MinGW-w64..." -ForegroundColor Cyan
        Invoke-WithRetry { winget install --id GNU.MinGW-w64 -e --source winget --accept-package-agreements --accept-source-agreements }
    } else { Write-Host "[OK] GCC найден. Пропуск." -ForegroundColor Green }

    if (!(Get-Command clang -ErrorAction SilentlyContinue)) {
        Write-Host "Скачивание LLVM..." -ForegroundColor Cyan
        Invoke-WithRetry { winget install --id LLVM.LLVM -e --source winget --accept-package-agreements --accept-source-agreements }
    } else { Write-Host "[OK] Clang найден. Пропуск." -ForegroundColor Green }
    
    Write-Host "[!] На Windows память (Valgrind) не чекается. Юзай WSL для тестов памяти!" -ForegroundColor DarkYellow
}

if ($choice -match '2|3') {
    Write-Host "`n[+] Проверка .NET 10 SDK..." -ForegroundColor Green
    
    $dotnetVer = if (Get-Command dotnet -ErrorAction SilentlyContinue) { (dotnet --version) } else { "" }
    if ($dotnetVer -notmatch "^10\.") {
        Write-Host "Скачивание/Обновление .NET 10 SDK..." -ForegroundColor Green
        Invoke-WithRetry { winget install --id Microsoft.DotNet.SDK.10 -e --source winget --accept-package-agreements --accept-source-agreements }
    } else { Write-Host "[OK] .NET 10 SDK ($dotnetVer) найден. Пропуск." -ForegroundColor Green }
}

$destDir = "$HOME\.uni-sentinel\bin"
if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }

$exePath = "$destDir\uni-sentinel.exe"
if (Test-Path $exePath) { Remove-Item -Path $exePath -Force }

Write-Host "`n[⬇️] Загрузка ядра Sentinel..." -ForegroundColor Cyan
$url = "https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-win.exe"

Invoke-WithRetry { Invoke-WebRequest -Uri $url -OutFile $exePath }
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
