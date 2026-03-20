# 🛡️ Uni-Sentinel (Native AOT Edition)

![Version](https://img.shields.io/badge/version-2.3.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10.0%20AOT-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20macOS%20%7C%20WSL-lightgrey)

**Uni-Sentinel** — это ультимативный, молниеносный инструмент для локального аудита кода и автоматизации CI/CD. Написанный на C# и скомпилированный в машинный код (Native AOT), он работает в 10 раз быстрее скриптовых аналогов.

Он сам установит нужные зависимости, найдет ваши `Makefile` или `.csproj`, проверит код на утечки памяти, стиль и строгие правила структурного программирования, а заодно — **прокачает ваш ранг за чистый код**.

> *Вы пишете код — Sentinel делает всю грязную работу по проверке.*

---

## 🚀 Установка (Одной командой)

Вставьте эту строку в терминал. Скрипт скачает исходники, скомпилирует бинарник и установит его в вашу систему глобально.

```bash
bash <(curl -s [https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh](https://raw.githubusercontent.com/irovbyte/Uni-Sentinel/main/install.sh))
```
*(Никаких перезагрузок оболочки или возни с алиасами. Инструмент сразу готов к бою).*

---

## 🎮 Геймификация: От Trainee до Shadow Monarch

Uni-Sentinel не просто ругается на ошибки. За каждый прогон проекта без единого нарушения вы получаете **+1 XP**. Накопленный опыт открывает новые ранги и полностью меняет эстетику вашего терминала.

* **0 XP:** `Trainee` (Серый)
* **1 XP:** `Awakened` (Ледяной синий)
* **25 XP:** `Lycoris Elite` (Кроваво-красный)
* **100+ XP:** `SHADOW MONARCH` (Абсолютная тьма)

---

## 🔥 Ключевые возможности

### 🤖 Умное ядро
* **Auto-Dependency:** Автоматически определяет ваш Linux-дистрибутив (Ubuntu, Arch, Fedora) и предлагает установить недостающие системные пакеты (`make`, `valgrind`, `clang`, `dotnet-sdk`).
* **Multi-Make Routing:** Находит ВСЕ `Makefile` в проекте (даже в подпапках), парсит цели и выстраивает очередь сборки (`all` -> `test` -> `gcov_report`).

### 💂 C Strict Guard
* **Стиль кода (Clang):** Автоматическая генерация и проверка `.clang-format`.
* **Структура кода (Принципы Дейкстры):** 
    * 🚫 Блокировка `goto`.
    * 📦 Вложенность блоков кода не более 4-х уровней.
    * 📏 Ограничение размера функций (не более 50 строк).
* **Инспекция Памяти:** Глубокая интеграция с `valgrind`. Умный парсинг `0 errors`.
* **Anti-Cheat Mode:** Опциональная блокировка использования небезопасных или запрещенных функций (`printf`, `strcpy` и т.д.) в production-файлах.

### 🟣 C# / .NET Enterprise Guard
* **Анализ Roslyn:** Запуск `dotnet format analyzers` для поиска Code Smells и плохих практик.
* **Безопасность (CVE):** Сканирование `NuGet`-пакетов на известные уязвимости.
* **Авто-форматирование:** Исправление отступов и стилистики на лету.

---

## 📖 Использование

Зайдите в папку с вашим проектом (C/C++ или C#) и введите:

```bash
uni-sentinel
```

### Команды:

| Команда | Описание |
| :--- | :--- |
| `uni-sentinel` | Запустить полную проверку директории |
| `uni-sentinel ac on` | **Включить** режим строгого контроля (Anti-Cheat) |
| `uni-sentinel ac off` | **Выключить** режим строгого контроля |
| `uni-sentinel update` | Скачать свежую версию из GitHub и пересобрать себя в фоне |
| `uni-sentinel help` | Показать справку |

---

<div align="center">
  <b>Forged in the shadows by <a href="https://github.com/irovbyte">irovbyte</a></b><br>
  <i>Powered by C# & .NET 10 Native AOT</i>
</div>
