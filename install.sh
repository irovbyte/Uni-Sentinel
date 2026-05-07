#!/bin/bash
APP_NAME="Uni-Sentinel"
BINARY_NAME="uni-sentinel"
INSTALL_DIR="/usr/local/bin"
TMP_DIR="/tmp/uni-sentinel-installer"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
PURPLE='\033[0;35m'
YELLOW='\033[0;33m'
NC='\033[0m'

echo -e "${PURPLE}======================================${NC}"
echo -e "${PURPLE}  🚀 ULTRA INSTALLER: ${APP_NAME} 🚀  ${NC}"
echo -e "${PURPLE}======================================${NC}"

echo -e "Какой стек технологий ты будешь использовать?"
echo -e "  1) ${BLUE}C / C++${NC} (School 21, GCC, Valgrind, Make)"
echo -e "  2) ${GREEN}C# / .NET${NC} (Core.AI, Web, Desktop)"
echo -e "  3) ${YELLOW}Всё и сразу${NC} (Fullstack Titan)"
read -p "Выбери номер [1-3]: " STACK_CHOICE

PM=""
if command -v apt-get &> /dev/null; then PM="apt-get"
elif command -v pacman &> /dev/null; then PM="pacman"
elif command -v dnf &> /dev/null; then PM="dnf"
fi

DEPS="git "
if [[ "$STACK_CHOICE" == "1" || "$STACK_CHOICE" == "3" ]]; then
    DEPS+="clang build-essential valgrind lcov cppcheck "
fi
if [[ "$STACK_CHOICE" == "2" || "$STACK_CHOICE" == "3" ]]; then
    if [ "$(dotnet --version 2>/dev/null | cut -d. -f1)" != "10" ]; then DEPS+="dotnet-sdk-10.0 "; fi
fi

if [ "$DEPS" != "git " ]; then
    echo -e "${YELLOW}[!] Устанавливаем зависимости: ${RED}$DEPS${NC}"
    if [ "$PM" == "apt-get" ]; then sudo apt-get update && sudo apt-get install -y $DEPS
    elif [ "$PM" == "pacman" ]; then sudo pacman -S --noconfirm $DEPS
    elif [ "$PM" == "dnf" ]; then sudo dnf install -y $DEPS
    fi
fi

echo -e "\n${BLUE}⬇️ Установка ядра...${NC}"
URL="https://github.com/irovbyte/Uni-Sentinel/releases/latest/download/uni-sentinel-linux"
# Если собираем из исходников, можно оставить твой git clone. Но для юзеров лучше качать бинарник напрямую:
sudo curl -L -q "$URL" -o "$INSTALL_DIR/$BINARY_NAME" || { echo -e "${RED}[ERR] Ошибка скачивания! Проверь GitHub Releases.${NC}"; exit 1; }
sudo chmod 755 "$INSTALL_DIR/$BINARY_NAME"

echo -e "\n${GREEN}✅ УСТАНОВКА ЗАВЕРШЕНА УСПЕШНО!${NC}"
echo -e "Попробуй команду: ${PURPLE}$BINARY_NAME help${NC}"
