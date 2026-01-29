import sys
import os

# Добавляем путь для модулей
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from core.logger import logger
from core.scanner import Scanner
from core.style_checker import StyleChecker
from core.builder import Builder
from core.functional_tests import FunctionalTester
from core.memory_checker import MemoryChecker

def main():
    logger.header("🚀 S21 ULTRA LINTER: SYSTEM START")
    
    # Ищем корень проекта (на уровень выше от папки линтера)
    project_root = ".." 
    
    # --- ЭТАП 0: ПОДГОТОВКА ---
    scanner = Scanner(project_root)
    
    # 1. Проверка ветки Git (develop)
    scanner.check_git_branch()
    
    # 2. Копирование .clang-format
    scanner.setup_linters()
    
    # 3. Поиск проектов
    projects = scanner.scan()
    
    if not projects:
        logger.fail("Проекты (Makefile + .c) не найдены. Работа завершена.")
        return

    logger.info(f"Найдено проектов в очереди: {len(projects)}")

    # Инициализация модулей
    style = StyleChecker()
    builder = Builder()
    tester = FunctionalTester()
    memory = MemoryChecker()

    # --- ЦИКЛ ПРОВЕРКИ ---
    for proj in projects:
        print("\n" + "-"*60)
        logger.info(f"🔥 НАЧАЛО ПРОВЕРКИ ПРОЕКТА: {proj['name']} ({proj['type']})")
        
        # Шаг 1: Стиль и Принципы (50 строк, goto, вложенность)
        if not style.check_project(proj):
            logger.fail("ПРОВАЛ: Стиль кода или структурные принципы нарушены.")
            continue 

        # Шаг 2: Сборка (Make)
        if not builder.build_project(proj):
            logger.fail("ПРОВАЛ: Проект не собирается или есть Warnings.")
            continue

        # Шаг 3: Тесты (Сравнение с bash или make test)
        if not tester.run_tests(proj):
            logger.warning("ПРОВАЛ: Функциональные тесты не прошли.")
        
        # Шаг 4: Память (Valgrind)
        if not memory.check_memory(proj):
            logger.warning("ПРОВАЛ: Найдены утечки памяти (Valgrind)!")

        # Шаг 5: Доп. проверка (AddressSanitizer)
        # Запускаем только если это программа, а не либа (для простоты)
        if proj['type'] == "CLI":
            builder.run_sanitizer_check(proj)
            
    # --- ИТОГИ ---
    logger.print_summary()

if __name__ == "__main__":
    main()