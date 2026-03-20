#!/bin/bash

APP_NAME="Uni-Sentinel"
BINARY_NAME="uni-sentinel"
REPO_URL="https://github.com/irovbyte/Uni-Sentinel.git"
TMP_DIR="/tmp/uni-sentinel-installer"
INSTALL_DIR="/usr/local/bin"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
PURPLE='\033[0;35m'
YELLOW='\033[0;33m'
NC='\033[0m'

echo -e "${PURPLE}======================================${NC}"
echo -e "${PURPLE}  🚀 УСТАНОВКА ${APP_NAME} (Native AOT) 🚀  ${NC}"
echo -e "${PURPLE}======================================${NC}"

PM=""
if command -v apt-get &> /dev/null; then PM="apt-get"
elif command -v pacman &> /dev/null; then PM="pacman"
elif command -v dnf &> /dev/null; then PM="dnf"
fi

MISSING_DEPS=""
if ! command -v git &> /dev/null; then MISSING_DEPS="git "; fi
if ! command -v clang &> /dev/null && ! command -v gcc &> /dev/null; then MISSING_DEPS+="clang "; fi
if ! command -v dotnet &> /dev/null; then MISSING_DEPS+="dotnet-sdk-10.0 "; fi

if [ -n "$MISSING_DEPS" ]; then
    echo -e "${YELLOW}[WARN] Отсутствуют системные пакеты: ${RED}$MISSING_DEPS${NC}"
    
    if [ -n "$PM" ]; then
        read -p "Желаете установить их автоматически через $PM? [y/N]: " choice
        if [[ "$choice" == [Yy]* ]]; then
            echo -e "${BLUE}⚙️ Запускаем установку зависимостей... (введите пароль)${NC}"
            if [ "$PM" == "apt-get" ]; then
                sudo apt-get update && sudo apt-get install -y git clang dotnet-sdk-10.0
            elif [ "$PM" == "pacman" ]; then
                sudo pacman -S --noconfirm git clang dotnet-sdk
            elif [ "$PM" == "dnf" ]; then
                sudo dnf install -y git clang dotnet-sdk-10.0
            fi
            echo -e "${GREEN}[OK] Зависимости установлены!${NC}"
        else
            echo -e "${RED}[ERR] Без этих пакетов установка невозможна. Отмена.${NC}"
            exit 1
        fi
    else
        echo -e "${RED}[ERR] Не удалось определить менеджер пакетов. Установите $MISSING_DEPS вручную.${NC}"
        exit 1
    fi
fi

echo -e "\n${BLUE}⬇️ Скачиваем исходники из GitHub...${NC}"
rm -rf "$TMP_DIR"
git clone -q "$REPO_URL" "$TMP_DIR"
cd "$TMP_DIR"

echo -e "${BLUE}⚙️ Компиляция в Native AOT (может занять около минуты)...${NC}"
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true -v q

if [ $? -ne 0 ]; then
    echo -e "${RED}[ERROR] Ошибка компиляции!${NC}"
    exit 1
fi

BINARY_PATH="bin/Release/net10.0/linux-x64/publish/UniSentinel"

echo -e "${BLUE}🛡️ Установка в системный каталог $INSTALL_DIR...${NC}"
sudo cp "$BINARY_PATH" "$INSTALL_DIR/$BINARY_NAME"
sudo chmod +x "$INSTALL_DIR/$BINARY_NAME"

echo -e "${BLUE}🧹 Удаление временных файлов...${NC}"
cd ~
rm -rf "$TMP_DIR"

echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}✅ Установка успешно завершена!${NC}"
echo -e "Теперь вы можете использовать команду '${PURPLE}$BINARY_NAME${NC}' в любой папке."
echo -e "${GREEN}======================================${NC}"