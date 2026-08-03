# 🛡️ Uni-Sentinel (Native AOT Edition)

![Version](https://img.shields.io/badge/version-3.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0%20AOT-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20WSL-lightgrey)

**Uni-Sentinel** — это ультимативный, молниеносный инструмент для локального аудита кода C# / .NET проектов, проверки стиля и генерации смарт-дампов для AI. Написанный на C# и скомпилированный в Native AOT машинный код, он работает невероятно быстро и не требует глобальной среды .NET на твоем устройстве для запуска самого инструмента.

Он автоматически загрузит нужные зависимости, найдет ваши `.csproj` файлы, проверит код, а заодно — **прокачает ваш ранг за чистый код**. Эксклюзивно для Linux/WSL.

> *Вы пишете код — Sentinel делает всю грязную работу по проверке.*

---

## 🚀 Быстрая установка

Запустите команду в терминале. Скрипт автоматически загрузит актуальное ядро и изолированно установит **Uni-Sentinel**. Поддерживаются только ОС на базе Linux / WSL.

**Linux / WSL**
```bash
bash <(curl -sL https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh)
```

> [!IMPORTANT]
> **Инструмент готов к бою сразу после установки.** Зависимости настраиваются изолированно, сохраняя вашу систему идеально чистой.

---

## 🎮 Геймификация: От Trainee до Core Singularity

Uni-Sentinel не просто ругается на ошибки. За каждый прогон проекта без единого нарушения вы получаете **+1 XP**. Накопленный опыт открывает новые ранги и полностью меняет эстетику вашего терминала. 

* **0 XP:** `Trainee` (Серый)
* **10 XP:** `Sentinel` (Зеленый)
* **25 XP:** `Cyber Runner` (Неоновый)
* **100 XP:** `VIPER BOSS` (Токсичный фиолетовый)
* **150+ XP:** `SHADOW MONARCH` (Абсолютная тьма и багровый акцент)
* **1000+ XP:** `CORE SINGULARITY` (Сингулярность идеального кода)

Все ваши достижения, очки и настройки аккуратно хранятся в папке `~/.uni-sentinel/config/`.

---

## 🔥 Ключевые возможности

### 🤖 Smart Dump 2.0
* **Smart Dump для AI (`dump`):** Генерирует чистый текстовый дамп всего проекта для загрузки в LLM (ChatGPT/Claude/Gemini). Читает файлы потоками, поэтому работает с проектами любого размера.
* **Черные списки:** Настройте исключения! В корне проекта создайте `.uni-sentinel_dump.json` для локальных правил, или добавьте глобальные исключения в `~/.uni-sentinel/config/dump_blacklist.json`.

### 🟣 C# / .NET Enterprise Guard
* **Анализ Roslyn:** Запуск `dotnet format analyzers` для поиска Code Smells.
* **Безопасность (CVE):** Сканирование `NuGet`-пакетов на уязвимости.
* **Global Shadow Mode:** Автоматическое исправление отступов без изменения ваших локальных конфигов (мы намеренно **вырезали .editorconfig**, чтобы не мешать вашей IDE).

---

## 📖 Использование CLI

Зайдите в папку с вашим проектом C# и введите:

```bash
uni-sentinel
```

### Доступные команды:

| Команда | Описание |
| :--- | :--- |
| `uni-sentinel audit` | (По умолчанию) Запустить полный аудит директории C# |
| `uni-sentinel dump` | Сгенерировать умный дамп кода (`.txt`) для LLM |
| `uni-sentinel install` | Установить ядро и зависимости |
| `uni-sentinel update` | Обновить ядро из GitHub (авто-загрузка релиза) |
| `uni-sentinel uninstall` | Удалить всю систему (`~/.uni-sentinel/`) |
| `uni-sentinel install-hook` | Защитить репозиторий (Git Pre-commit интеграция) |

---

<div align="center">
  <b>Forged in the shadows by <a href="https://github.com/irovbyte">irovbyte</a></b><br>
  <i>Powered by C# & .NET 10 Native AOT</i>
</div>
