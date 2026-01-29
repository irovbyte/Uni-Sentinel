#!/bin/bash

# Название проекта
APP_NAME="C-Sentinel"
INSTALL_DIR=".c_sentinel"

# ССЫЛКУ НИЖЕ ЗАМЕНИШЬ НА СВОЙ НОВЫЙ РЕПОЗИТОРИЙ
REPO_URL="https://github.com/YOUR_USERNAME/C-Sentinel.git"

# Цвета
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}    🛡️  ${APP_NAME} INSTALLER  🛡️    ${NC}"
echo -e "${BLUE}=========================================${NC}"

# Проверка Python
if ! command -v python3 &> /dev/null; then
    echo -e "${RED}[ERROR] Python3 не установлен!${NC}"
    exit 1
fi

# Установка
if [ -d "$INSTALL_DIR" ]; then
    echo -e "${GREEN}🔄 Обновление ${APP_NAME}...${NC}"
    cd "$INSTALL_DIR" && git pull && cd ..
else
    echo -e "${GREEN}⬇️ Скачивание ${APP_NAME}...${NC}"
    git clone -q "$REPO_URL" "$INSTALL_DIR"
fi

# Скрываем папку от Git проекта, в котором мы находимся
if [ -f ".gitignore" ]; then
    if ! grep -q "$INSTALL_DIR" ".gitignore"; then
        echo "$INSTALL_DIR" >> .gitignore
        echo -e "${GREEN}✅ Папка $INSTALL_DIR добавлена в .gitignore${NC}"
    fi
else
    echo "$INSTALL_DIR" > .gitignore
fi

# Запуск
echo ""
echo -e "${BLUE}Установка завершена!${NC}"
read -p "Запустить проверку прямо сейчас? [Y/n] " response
response=${response:-Y}

if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    echo -e "\n${GREEN}🚀 Запускаем Стража...${NC}\n"
    python3 "$INSTALL_DIR/main.py"
else
    echo -e "\n${BLUE}Чтобы запустить позже, используй:${NC}"
    echo -e "python3 $INSTALL_DIR/main.py"
fi