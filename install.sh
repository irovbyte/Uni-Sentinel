#!/bin/bash

# Настройки
APP_NAME="Uni-Sentinel"
INSTALL_DIR="$HOME/.uni-sentinel"

REPO_URL="https://github.com/irovbyte/Uni-Sentinel.git"

# Цвета
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}======================================${NC}"
echo -e "${BLUE}   🚀 УСТАНОВКА ${APP_NAME} 🚀      ${NC}"
echo -e "${BLUE}======================================${NC}"

# 1. Проверка зависимостей
if ! command -v python3 &> /dev/null; then
    echo -e "${RED}[ERROR] Python3 не установлен!${NC}"
    exit 1
fi

if ! command -v git &> /dev/null; then
    echo -e "${RED}[ERROR] Git не установлен!${NC}"
    exit 1
fi

# 2. Клонирование репозитория
if [ -d "$INSTALL_DIR" ]; then
    echo -e "${BLUE}🔄 Папка существует. Обновляем репозиторий...${NC}"
    cd "$INSTALL_DIR" && git pull && cd - > /dev/null
else
    echo -e "${GREEN}⬇️ Скачиваем в $INSTALL_DIR...${NC}"
    git clone -q "$REPO_URL" "$INSTALL_DIR"
fi

# 3. Настройка Shell (Alias)
SHELL_CONFIG=""
if [ -n "$ZSH_VERSION" ]; then
    SHELL_CONFIG="$HOME/.zshrc"
elif [ -n "$BASH_VERSION" ]; then
    SHELL_CONFIG="$HOME/.bashrc"
else
    # Пробуем угадать по файлам
    if [ -f "$HOME/.zshrc" ]; then
        SHELL_CONFIG="$HOME/.zshrc"
    elif [ -f "$HOME/.bashrc" ]; then
        SHELL_CONFIG="$HOME/.bashrc"
    fi
fi


if [ -n "$SHELL_CONFIG" ]; then
    # Проверяем, есть ли уже алиас
    if ! grep -q "alias uni-sentinel=" "$SHELL_CONFIG"; then
        echo "" >> "$SHELL_CONFIG"
        echo "# Alias for Uni-Sentinel" >> "$SHELL_CONFIG"
        echo "alias uni-sentinel='python3 $INSTALL_DIR/main.py'" >> "$SHELL_CONFIG"
        echo -e "${GREEN}✅ Алиас добавлен в $SHELL_CONFIG${NC}"
    else
        echo -e "${GREEN}✅ Алиас уже был настроен.${NC}"
    fi
else
    echo -e "${RED}[WARN] Не удалось определить конфиг шелла (.bashrc/.zshrc).${NC}"
    echo -e "Добавьте вручную: alias uni-sentinel='python3 $INSTALL_DIR/main.py'"
    exit 0
fi

echo -e "\n${GREEN}Установка завершена!${NC}"
echo -e "Сейчас мы перезагрузим оболочку, чтобы команда '${BLUE}uni-sentinel${NC}' заработала сразу."

# Магия перезапуска
if [ -n "$SHELL" ]; then
    exec "$SHELL" -l
else
    echo -e "${BLUE}⚠️  Пожалуйста, выполните: source $SHELL_CONFIG${NC}"
fi