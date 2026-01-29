import os
import re
import subprocess
import sys

# Хак для импорта конфига
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from config import settings
from core.logger import logger

class StyleChecker:
    def __init__(self):
        pass

    def check_project(self, project_data):
        """Запускает полную проверку стиля для проекта"""
        files = project_data['files']
        project_name = project_data['name']
        
        logger.header(f"ЭТАП 2: STYLE & PRINCIPLES ({project_name})")
        
        all_passed = True

        # 1. Проверка CLANG-FORMAT (Google Style)
        logger.info("Запуск Clang-format...")
        if not self._run_clang_format(files):
            all_passed = False

        # 2. Проверка ПРИНЦИПОВ (Вложенность, длина функций, goto)
        logger.info("Проверка принципов структурного программирования...")
        if not self._check_principles(files):
            all_passed = False
            
        return all_passed

    def _run_clang_format(self, files):
        """Обертка над clang-format -n"""
        has_errors = False
        
        # Проверяем, установлен ли clang-format
        try:
            subprocess.run(["clang-format", "--version"], capture_output=True, check=True)
        except (FileNotFoundError, subprocess.CalledProcessError):
            logger.fail("Clang-format не установлен! (sudo apt install clang-format)")
            return False

        for file_path in files:
            # -n означает dry-run (не менять файл, просто показать ошибки)
            # --Werror превращает предупреждения в ошибки
            cmd = ["clang-format", "-n", "--Werror", file_path]
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if result.returncode != 0:
                logger.fail(f"Ошибка стиля (Google Style) в файле: {os.path.basename(file_path)}")
                # Можно раскомментировать, чтобы видеть детали:
                # print(result.stderr) 
                has_errors = True
        
        if not has_errors:
            logger.success("Clang-format: OK")
        
        return not has_errors

    def _check_principles(self, files):
        """
        Самописный парсер для проверки:
        1. Длины функций (<= 50 строк)
        2. Вложенности (<= 4 уровней)
        3. Запрещенных слов (goto)
        """
        all_ok = True
        
        for file_path in files:
            # Пропускаем .h файлы для проверки длины функций (там обычно прототипы)
            if file_path.endswith(".h"):
                continue

            try:
                with open(file_path, 'r') as f:
                    lines = f.readlines()
            except Exception as e:
                logger.fail(f"Не могу прочитать {file_path}: {e}")
                continue

            # --- ПЕРЕМЕННЫЕ СОСТОЯНИЯ ---
            brace_balance = 0       # Баланс фигурных скобок { }
            in_function = False     # Мы внутри функции?
            func_start_line = 0     # Где началась функция
            current_lines = 0       # Текущая длина функции
            nesting_level = 0       # Текущая вложенность
            
            for i, line in enumerate(lines):
                line_num = i + 1
                stripped = line.strip()
                
                # Игнорируем комментарии и пустые строки
                if not stripped or stripped.startswith("//") or stripped.startswith("/*"):
                    continue

                # 1. ПРОВЕРКА GOTO
                for bad_word in settings.FORBIDDEN_KEYWORDS:
                    # Простая проверка слова (с границами слов)
                    if re.search(fr'\b{bad_word}\b', stripped):
                        logger.fail(f"{os.path.basename(file_path)}:{line_num} -> Использовано запрещенное слово '{bad_word}'")
                        all_ok = False

                # 2. ПОДСЧЕТ СКОБОК И ВЛОЖЕННОСТИ
                open_braces = stripped.count('{')
                close_braces = stripped.count('}')
                
                # Обновляем баланс
                brace_balance += open_braces
                brace_balance -= close_braces

                # Логика вложенности:
                # Если баланс растет внутри функции — растет вложенность
                if in_function:
                    nesting_level += open_braces
                    nesting_level -= close_braces
                    
                    if nesting_level > settings.MAX_NESTING_LEVEL:
                        logger.warning(f"{os.path.basename(file_path)}:{line_num} -> Высокая вложенность ({nesting_level})! Рекомендуется упростить.")
                        # Это warning, не fail, так как иногда неизбежно, но лучше исправить

                # 3. ЛОГИКА ФУНКЦИЙ
                # Если баланс стал 1 и мы не были в функции — похоже, функция началась
                # (Исключаем struct, enum, typedef, так как они тоже используют {})
                if brace_balance > 0 and not in_function:
                    if not (stripped.startswith("struct") or stripped.startswith("typedef") or stripped.startswith("enum")):
                        in_function = True
                        func_start_line = line_num
                        current_lines = 0
                        nesting_level = 1 # Сброс вложенности для новой функции

                if in_function:
                    current_lines += 1

                # Если баланс вернулся в 0 — функция закончилась
                if brace_balance == 0 and in_function:
                    # Проверка длины
                    if current_lines > settings.MAX_LINES_PER_FUNC:
                        logger.fail(f"{os.path.basename(file_path)}: Функция на стр. {func_start_line} занимает {current_lines} строк (MAX {settings.MAX_LINES_PER_FUNC})")
                        all_ok = False
                    
                    in_function = False
                    current_lines = 0

        if all_ok:
            logger.success("Принципы структурного программирования соблюдены!")
        
        return all_ok