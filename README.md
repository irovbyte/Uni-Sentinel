# 🛡️ Uni-Sentinel (Native AOT Edition)

![Version](https://img.shields.io/badge/version-3.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0%20AOT-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20macOS%20%7C%20Windows-lightgrey)

**Uni-Sentinel** — это ультимативный, молниеносный инструмент для локального аудита кода и автоматизации CI/CD. Написанный на C# и скомпилированный в машинный код (Native AOT), он работает в разы быстрее скриптовых аналогов и не требует среды выполнения .NET на устройстве пользователя.

Он сам установит нужные зависимости, найдет ваши `Makefile` или `.csproj`, проверит код на утечки памяти, стиль и строгие правила структурного программирования, а заодно — **прокачает ваш ранг за чистый код**.

> *Вы пишете код — Sentinel делает всю грязную работу по проверке.*

---

## 🚀 Быстрая установка

Выберите вашу ОС и вставьте команду в терминал. Скрипт автоматически загрузит актуальное ядро, настроит системные пути и подготовит **Uni-Sentinel** к работе.

**Linux / WSL / macOS**
```bash
bash <(curl -sL [https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh](https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh))
```

**Windows (PowerShell)**
```powershell
powershell -ExecutionPolicy ByPass -c "irm [https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.ps1](https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.ps1) | iex"
```

> [!IMPORTANT]
> **Инструмент готов к бою сразу после выполнения команды.** Никаких ручных правок `.bashrc`, переменных среды или возни с алиасами. 

---

## 🎮 Геймификация: От Trainee до Core Singularity

Uni-Sentinel не просто ругается на ошибки. За каждый прогон проекта без единого нарушения вы получаете **+1 XP**. Накопленный опыт открывает новые ранги и полностью меняет эстетику вашего терминала. Система отслеживает ваши стрики (ежедневную активность) и дает множители опыта!

* **0 XP:** `Trainee` (Серый)
* **10 XP:** `Sentinel` (Зеленый)
* **25 XP:** `Cyber Runner` (Неоновый)
* **100 XP:** `VIPER BOSS` (Токсичный фиолетовый)
* **150+ XP:** `SHADOW MONARCH` (Абсолютная тьма и багровый акцент)

---

## 🔥 Ключевые возможности

### 🤖 Умное ядро
* **Auto-Dependency:** Автоматически определяет вашу ОС (Windows, Ubuntu, Arch, Fedora) и устанавливает недостающие пакеты (через `winget`, `apt`, `pacman` или `dnf`).
* **Smart Dump:** Команда для генерации чистого текстового слепка всего проекта (игнорируя мусор) для передачи контекста в LLM.
* **Multi-Make Routing:** Находит ВСЕ `Makefile` в проекте, парсит цели и интеллектуально выстраивает очередь сборки.

### 💂 C Strict Guard
* **Стиль кода (Clang):** Автоматическая генерация конфигурации и жесткая проверка через `clang-format` (Google / C11).
* **Принципы Дейкстры:** * 🚫 Полная блокировка `goto`.
    * 📦 Вложенность блоков кода не более 4-х уровней.
    * 📏 Ограничение размера функций (не более 50 строк).
* **Инспекция Памяти:** Глубокая интеграция с `valgrind` (поиск утечек).
* **Anti-Cheat Mode:** Опциональная блокировка опасных стандартных функций (`printf`, `strcpy`, `scanf` и др.) в релизных файлах.

### 🟣 C# / .NET Enterprise Guard
* **Анализ Roslyn:** Запуск `dotnet format analyzers` для поиска Code Smells и устранения плохих практик.
* **Безопасность (CVE):** Сканирование `NuGet`-пакетов на известные уязвимости.
* **Авто-форматирование:** Исправление отступов и стилистики на лету (Global Shadow Mode).

---

## 📖 Использование CLI

Зайдите в папку с вашим проектом (C/C++ или C#) и введите:

```bash
uni-sentinel
```

### Доступные команды:

| Команда | Описание |
| :--- | :--- |
| `uni-sentinel` | Запустить полный аудит текущей директории |
| `uni-sentinel dump` | Сгенерировать умный дамп кода (`.txt`) для LLM |
| `uni-sentinel install-hook` | Защитить репозиторий (Git Pre-commit интеграция) |
| `uni-sentinel ac on` | **Включить** режим Анти-Чит |
| `uni-sentinel ac off` | **Выключить** режим Анти-Чит |
| `uni-sentinel update` | Обновить ядро из GitHub (авто-пересборка) |
| `uni-sentinel uninstall` | Полностью удалить систему и сбросить прогресс |
| `uni-sentinel help` | Показать справку по командам |

---

<div align="center">
  <b>Forged in the shadows by <a href="https://github.com/irovbyte">irovbyte</a></b><br>
  <i>Powered by C# & .NET 10 Native AOT</i>
</div>
```
