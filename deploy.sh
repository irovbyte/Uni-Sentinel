#!/bin/bash

APP_NAME="uni-sentinel"
BINARY_PATH="bin/Release/net10.0/linux-x64/publish/UniSentinel"
INSTALL_DIR="/usr/local/bin"

echo -e "\x1b[38;5;93m[DEPLOY]\x1b[0m Запуск сборки $APP_NAME..."

if ! command -v clang &> /dev/null && ! command -v gcc &> /dev/null; then
    echo -e "\x1b[31m[ERR]\x1b[0m Не найден clang/gcc. Установи: sudo apt install build-essential clang"
    exit 1
fi

dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true

if [ $? -eq 0 ]; then
    echo -e "\x1b[32m[OK]\x1b[0m Сборка завершена успешно."

    echo -e "\x1b[38;5;135m[INSTALL]\x1b[0m Копирую бинарник в $INSTALL_DIR..."
    sudo cp "$BINARY_PATH" "$INSTALL_DIR/$APP_NAME"
    sudo chmod +x "$INSTALL_DIR/$APP_NAME"
    
    echo -e "\x1b[32m[SUCCESS]\x1b[0m Теперь ты можешь использовать команду '$APP_NAME' везде!"
else
    echo -e "\x1b[31m[ERR]\x1b[0m Ошибка компиляции."
    exit 1
fi