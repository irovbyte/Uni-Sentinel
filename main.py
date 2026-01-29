# main.py
import sys
import os
import subprocess
from config import settings
from core.logger import logger

# Добавляем текущую директорию в путь, чтобы импорты работали
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

def self_update():
    """Функция самообновления через Git"""
    logger.header("ЗАПУСК ОБНОВЛЕНИЯ UNI-SENTINEL")
    
    # Путь к папке, где лежит скрипт
    root_dir = os.path.dirname(os.path.abspath(__file__))
    
    try:
        # Проверяем, это git репозиторий?
        if not os.path.exists(os.path.join(root_dir, ".git")):
            logger.fail("Это не Git-репозиторий. Обновление невозможно.")
            return

        logger.info("Скачиваем изменения с GitHub...")
        res = subprocess.run(["git", "pull"], cwd=root_dir, capture_output=True, text=True)
        
        if res.returncode == 0:
            if "Already up to date" in res.stdout:
                logger.success("У вас уже установлена последняя версия!")
            else:
                logger.success("Обновление завершено успешно! Перезапустите команду.")
                print(res.stdout)
        else:
            logger.fail("Ошибка при обновлении:")
            print(res.stderr)
            
    except Exception as e:
        logger.fail(f"Критическая ошибка обновления: {e}")

def run_scan():
    """Заглушка для запуска сканирования (допишем позже)"""
    logger.header(f"ЗАПУСК {settings.APP_NAME} v{settings.VERSION}")
    logger.info("Сканирование текущей директории...")
    # Тут будет вызов Scanner().scan()
    print("🚧 (Сканер будет подключен на следующем этапе) 🚧")

def print_help():
    print(f"""
{settings.Colors.BOLD}{settings.APP_NAME} v{settings.VERSION}{settings.Colors.ENDC}
Использование:
    uni-sentinel         -> Запустить проверку в текущей папке
    uni-sentinel update  -> Обновить утилиту до последней версии
    uni-sentinel help    -> Показать это сообщение
    """)

def main():
    if len(sys.argv) > 1:
        command = sys.argv[1]
        if command == "update":
            self_update()
        elif command == "help":
            print_help()
        else:
            logger.fail(f"Неизвестная команда: {command}")
            print_help()
    else:
        # Если аргументов нет — запускаем сканер
        run_scan()

if __name__ == "__main__":
    main()