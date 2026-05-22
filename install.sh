#!/bin/bash
set -e

C_RESET='\033[0m'
C_TITLE='\033[1;38;5;93m'
C_MAIN='\033[38;5;159m'
C_WARN='\033[38;5;222m'
C_OK='\033[38;5;150m'

echo -e "${C_TITLE}======================================${C_RESET}"
echo -e "${C_TITLE}  🚀 UNI-SENTINEL AUTO-INSTALLER 🚀   ${C_RESET}"
echo -e "${C_TITLE}======================================${C_RESET}\n"

echo -e "Какой стек технологий защищаем, Shadow Monarch?"
echo -e "  1) ${C_MAIN}C / C++${C_RESET} (Make, GCC, Valgrind)"
echo -e "  2) ${C_OK}C# / .NET${C_RESET} (.NET 10 SDK)"
echo -e "  3) ${C_TITLE}HTML / Blazor${C_RESET} (npm, prettier, xamlstyler)"
echo -e "  4) ${C_WARN}Всё и сразу${C_RESET} (Titan Mode)"
read -p "Выбери номер [1-4]: " STACK_CHOICE </dev/tty

PM=""
if command -v apt-get &> /dev/null; then PM="apt-get"
elif command -v pacman &> /dev/null; then PM="pacman"
elif command -v dnf &&> /dev/null; then PM="dnf"
fi

DEPS="git curl "
if [[ "$STACK_CHOICE" == "1" || "$STACK_CHOICE" == "3" ]]; then
    DEPS+="clang build-essential valgrind lcov cppcheck "
fi
if [[ "$STACK_CHOICE" == "2" || "$STACK_CHOICE" == "3" ]]; then
    if ! command -v dotnet &> /dev/null || [ "$(dotnet --version 2>/dev/null | cut -d. -f1)" != "10" ]; then
        DEPS+="dotnet-sdk-10.0 "
    fi
fi
if [[ "$STACK_CHOICE" == "3" || "$STACK_CHOICE" == "4" ]]; then
    if ! command -v npm &> /dev/null; then
        DEPS+="npm "
    fi
fi

if [ "$DEPS" != "git curl " ] && [ -n "$PM" ]; then
    echo -e "\n${C_WARN}[!] Устанавливаем зависимости: ${C_RESET}$DEPS"
    if [ "$PM" == "apt-get" ]; then sudo apt-get update && sudo apt-get install -y $DEPS
    elif [ "$PM" == "pacman" ]; then sudo pacman -S --noconfirm $DEPS
    elif [ "$PM" == "dnf" ]; then sudo dnf install -y $DEPS
    fi
fi

echo -e "\n${C_MAIN}[⬇️] Подготовка и скачивание ядра Uni-Sentinel...${C_RESET}"
URL="https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-linux"

sudo rm -f /usr/local/bin/uni-sentinel
rm -f ~/.local/bin/uni-sentinel

if sudo curl -sL "$URL" -o /usr/local/bin/uni-sentinel; then
    sudo chmod +x /usr/local/bin/uni-sentinel
    echo -e "${C_OK}[OK] Установлено глобально в /usr/local/bin${C_RESET}"
else
    echo -e "${C_WARN}[!] Нет sudo. Устанавливаю локально в ~/.local/bin${C_RESET}"
    mkdir -p ~/.local/bin
    curl -sL "$URL" -o ~/.local/bin/uni-sentinel
    chmod +x ~/.local/bin/uni-sentinel

    if [[ ":$PATH:" != *":$HOME/.local/bin:"* ]]; then
        echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
        echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
        export PATH="$HOME/.local/bin:$PATH"
    fi
fi

if [[ "$STACK_CHOICE" == "3" || "$STACK_CHOICE" == "4" ]]; then
    echo -e "\n${C_TITLE}[+] Установка UI-инструментов...${C_RESET}"
    if ! command -v xstyler &> /dev/null; then
        dotnet tool install -g xamlstyler.console || true
    fi
    if ! command -v prettier &> /dev/null; then
        sudo npm install -g prettier @prettier/plugin-xml || true
    fi
fi

echo -e "\n${C_OK}✅ УСТАНОВКА ЗАВЕРШЕНА!${C_RESET}"
echo -e "Твой прогресс сохранен. Попробуй: ${C_TITLE}uni-sentinel help${C_RESET}\n"
