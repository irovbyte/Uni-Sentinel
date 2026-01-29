import sys
import os
import subprocess
from config import settings
from core.logger import logger
from core.scanner import Scanner

# Добавляем текущую директорию в путь
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

def self_update():
    """Функция самообновления через Git"""
    logger.header("ЗАПУСК ОБНОВЛЕНИЯ UNI-SENTINEL")
    root_dir = os.path.dirname(os.path.abspath(__file__))
    
    try:
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
    """Запуск сканирования текущей директории"""
    logger.header(f"ЗАПУСК {settings.APP_NAME} v{settings.VERSION}")
    
    current_dir = os.getcwd()
    scanner = Scanner(current_dir)
    handler = scanner.detect_handler()
    
    if not handler:
        logger.fail("Не удалось определить тип проекта (нет файлов .c или .py).")
        return

    logger.info(f"Запуск проверки в: {current_dir}")
    
    success = True
    
    try:
        # Проверки
        if not handler.check_style(): success = False
        
        # Если стиль провален, все равно пробуем билдить (часто полезно)
        if not handler.build(): 
            success = False
            # Если билд упал, тесты нет смысла запускать
        else:
            # Запускаем тесты и память только если билд успешен
            if not handler.run_tests(): success = False
            if not handler.check_memory(): success = False
        
    except KeyboardInterrupt:
        print("\n")
        logger.warning("Проверка прервана пользователем.")
        success = False
    except Exception as e:
        logger.fail(f"Произошла ошибка в работе скрипта: {e}")
        success = False
    finally:
        # ЭТОТ БЛОК ВЫПОЛНИТСЯ ВСЕГДА (Очистка)
        print("") 
        handler.cleanup()
    
    # Итог
    print("\n" + "="*40)
    if success:
        logger.success("ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ! ТЫ ГОТОВ К ЗАЩИТЕ! 😎")
    else:
        logger.fail("ЕСТЬ ОШИБКИ. ИСПРАВЬ ИХ ПЕРЕД СДАЧЕЙ.")
    print("="*40 + "\n")

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
        run_scan()

if __name__ == "__main__":
    main()