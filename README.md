# 🛡️ Uni-Sentinel (Native AOT Edition)

![Version](https://img.shields.io/badge/version-3.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0%20AOT-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20Windows-lightgrey)

**Uni-Sentinel** — это ультимативный, молниеносный инструмент для локального аудита кода, проверки стиля и генерации смарт-дампов для AI. Написанный на C# и скомпилированный в Native AOT машинный код, он работает в разы быстрее скриптовых аналогов и не требует установленной среды .NET на твоем устройстве.

Он сам установит нужные портативные зависимости (без мусора в системе!), найдет ваши `Makefile` или `.csproj`, проверит код на утечки памяти, стиль и строгие правила структурного программирования, а заодно — **прокачает ваш ранг за чистый код**.

> *Вы пишете код — Sentinel делает всю грязную работу по проверке.*

---

## 🚀 Быстрая установка

Выберите вашу ОС и вставьте команду в терминал. Скрипт автоматически загрузит актуальное ядро и изолированно установит **Uni-Sentinel**. 

**Linux / WSL**
```bash
bash <(curl -sL https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh)
```

**Windows (PowerShell)**
```powershell
powershell -ExecutionPolicy ByPass -c "irm https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.ps1 | iex"
```

> [!IMPORTANT]
> **Инструмент готов к бою сразу после установки.** На Windows больше не используется winget: все зависимости C/C++ скачиваются в портативном виде локально в `~/.uni-sentinel/tools`, сохраняя вашу систему идеально чистой. Никаких следов в меню Пуск!

---

## 🎮 Геймификация: От Trainee до Core Singularity

Uni-Sentinel не просто ругается на ошибки. За каждый прогон проекта без единого нарушения вы получаете **+1 XP**. Накопленный опыт открывает новые ранги и полностью меняет эстетику вашего терминала. Система отслеживает ваши стрики (ежедневную активность) и дает множители опыта!

* **0 XP:** `Trainee` (Серый)
* **10 XP:** `Sentinel` (Зеленый)
* **25 XP:** `Cyber Runner` (Неоновый)
* **100 XP:** `VIPER BOSS` (Токсичный фиолетовый)
* **150+ XP:** `SHADOW MONARCH` (Абсолютная тьма и багровый акцент)
* **1000+ XP:** `CORE SINGULARITY` (Сингулярность идеального кода)

Все ваши достижения, очки и настройки аккуратно хранятся в папке `~/.uni-sentinel/config/`.

---

## 🔥 Ключевые возможности

### 🤖 Умное ядро и Smart Dump 2.0
* **Isolated Dependencies:** Автоматически скачивает и изолированно настраивает `LLVM` и `MinGW` на Windows или использует системные пакеты Linux. Никаких конфликтов PATH!
* **Smart Dump для AI (`dump`):** Генерирует чистый текстовый дамп всего проекта для загрузки в LLM (ChatGPT/Claude/Gemini). Читает файлы потоками, так что памяти хватит даже на проекты в 10+ ГБ.
* **Черные списки:** Настройте исключения! В корне проекта создайте `.uni-sentinel_dump.json` для локальных правил, или добавьте глобальные исключения в `~/.uni-sentinel/config/dump_blacklist.json`.

### 💂 C Strict Guard
* **Стиль кода (Clang):** Жесткая проверка через `clang-format`.
* **Принципы Дейкстры:** 
    * 🚫 Полная блокировка `goto`.
    * 📦 Вложенность блоков кода не более 4-х уровней.
    * 📏 Ограничение размера функций (не более 50 строк).
* **Инспекция Памяти:** Глубокая интеграция с `valgrind` (поиск утечек).
* **Anti-Cheat Mode:** Опциональная блокировка опасных стандартных функций (`printf`, `strcpy`, `scanf` и др.) в релизных файлах.

### 🟣 C# / .NET Enterprise Guard
* **Анализ Roslyn:** Запуск `dotnet format analyzers` для поиска Code Smells.
* **Безопасность (CVE):** Сканирование `NuGet`-пакетов на уязвимости.
* **Global Shadow Mode:** Автоматическое исправление отступов без изменения ваших локальных конфигов (мы намеренно **вырезали .editorconfig**, чтобы не мешать вашей IDE).

---

## 📖 Использование CLI

Зайдите в папку с вашим проектом (C/C++ или C#) и введите:

```bash
uni-sentinel
```

### Доступные команды:

| Команда | Описание |
| :--- | :--- |
| `uni-sentinel audit` | (По умолчанию) Запустить полный аудит директории |
| `uni-sentinel dump` | Сгенерировать умный дамп кода (`.txt`) для LLM |
| `uni-sentinel install` | Установить ядро и портативные зависимости |
| `uni-sentinel update` | Обновить ядро из GitHub (авто-пересборка) |
| `uni-sentinel uninstall` | Удалить всю систему (`~/.uni-sentinel/`) |
| `uni-sentinel install-hook` | Защитить репозиторий (Git Pre-commit интеграция) |
| `uni-sentinel ac on` | **Включить** режим Анти-Чит |
| `uni-sentinel ac off` | **Выключить** режим Анти-Чит |

---

<div align="center">
  <b>Forged in the shadows by <a href="https://github.com/irovbyte">irovbyte</a></b><br>
  <i>Powered by C# & .NET 10 Native AOT</i>
</div>
