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
echo -e "${PURPLE}  🚀 ULTRA INSTALLER: ${APP_NAME} 🚀  ${NC}"
echo -e "${PURPLE}======================================${NC}"

PM=""
if command -v apt-get &> /dev/null; then PM="apt-get"
elif command -v pacman &> /dev/null; then PM="pacman"
elif command -v dnf &> /dev/null; then PM="dnf"
fi

MISSING_DEPS=""
DOTNET_VER=$(dotnet --version 2>/dev/null | cut -d. -f1)

if ! command -v git &> /dev/null; then MISSING_DEPS+="git "; fi
if ! command -v clang &> /dev/null && ! command -v gcc &> /dev/null; then MISSING_DEPS+="clang build-essential "; fi
if [ "$DOTNET_VER" != "10" ]; then MISSING_DEPS+="dotnet-sdk-10.0 "; fi

if [ -n "$MISSING_DEPS" ]; then
    echo -e "${YELLOW}[!] Необходимы зависимости: ${RED}$MISSING_DEPS${NC}"
    if [ -n "$PM" ]; then
        read -p "Установить всё автоматически? [y/N]: " choice
        if [[ "$choice" == [Yy]* ]]; then
            if [ "$PM" == "apt-get" ]; then
                sudo apt-get update && sudo apt-get install -y git clang build-essential dotnet-sdk-10.0
            elif [ "$PM" == "pacman" ]; then
                sudo pacman -S --noconfirm git clang dotnet-sdk
            elif [ "$PM" == "dnf" ]; then
                sudo dnf install -y git clang dotnet-sdk-10.0
            fi
        else
            echo -e "${RED}[ERR] Отмена установки.${NC}"; exit 1
        fi
    fi
fi

echo -e "\n${BLUE}⬇️ Клонирование репозитория...${NC}"
rm -rf "$TMP_DIR"
git clone -q "$REPO_URL" "$TMP_DIR" || { echo -e "${RED}[ERR] Ошибка сети${NC}"; exit 1; }
cd "$TMP_DIR"

echo -e "${BLUE}⚙️ Компиляция Native AOT (net10.0)...${NC}"
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true -p:InvariantGlobalization=true -f net10.0 -v q

if [ $? -ne 0 ]; then
    echo -e "${RED}[ERR] Сборка провалилась!${NC}"; exit 1
fi

BINARY_PATH="bin/Release/net10.0/linux-x64/publish/UniSentinel"
echo -e "${BLUE}🛡️ Регистрация в системе...${NC}"
sudo cp "$BINARY_PATH" "$INSTALL_DIR/$BINARY_NAME"
sudo chmod 755 "$INSTALL_DIR/$BINARY_NAME"
sudo chown root:root "$INSTALL_DIR/$BINARY_NAME"

echo -e "${BLUE}🔗 Настройка путей (PATH)...${NC}"
for CONFIG in "$HOME/.zshrc" "$HOME/.bashrc"; do
    if [ -f "$CONFIG" ]; then
        if ! grep -q "$INSTALL_DIR" "$CONFIG"; then
            echo "export PATH=\"\$PATH:$INSTALL_DIR\"" >> "$CONFIG"
            echo -e "${GREEN}  - Путь добавлен в $CONFIG${NC}"
        fi
    fi
done

cd ~
rm -rf "$TMP_DIR"

echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}✅ УСТАНОВКА ЗАВЕРШЕНА УСПЕШНО!${NC}"
echo -e "${YELLOW}ВАЖНО: Перезапусти терминал или введи: source ~/.zshrc${NC}"
echo -e "Попробуй команду: ${PURPLE}$BINARY_NAME help${NC}"
echo -e "${GREEN}======================================${NC}"