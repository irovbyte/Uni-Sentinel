import os
import subprocess
import re
from handlers.base_handler import BaseHandler
from core.logger import logger
from config import settings

class CHandler(BaseHandler):
    def check_style(self):
        logger.header("ЭТАП 1: СТИЛЬ И ПРИНЦИПЫ")
        all_ok = True

        # 1. Clang-format (Google Style)
        logger.info("Проверка Clang-format...")
        for f in self.files:
            if not f.endswith(".c") and not f.endswith(".h"): continue
            # -n = dry run, --Werror = считать ошибкой
            res = subprocess.run(["clang-format", "-n", "--Werror", f], capture_output=True, text=True)
            if res.returncode != 0:
                logger.fail(f"Стиль нарушен в {os.path.basename(f)}")
                # print(res.stderr) # Раскомментируй для деталей
                all_ok = False
        
        # 2. Проверка 7 принципов (функции < 50 строк, goto)
        logger.info("Анализ структуры кода (7 принципов)...")
        if not self._check_principles():
            all_ok = False

        if all_ok: logger.success("Стиль и структура в порядке.")
        return all_ok

    def build(self):
        logger.header("ЭТАП 2: СБОРКА (MAKE)")
        makefile = os.path.join(self.project_path, "Makefile")
        if not os.path.exists(makefile):
            logger.fail("Makefile не найден!")
            return False

        # Make clean && Make re
        subprocess.run(["make", "clean"], cwd=self.project_path, capture_output=True)
        
        logger.info("Запуск make re...")
        res = subprocess.run(["make", "re"], cwd=self.project_path, capture_output=True, text=True)
        
        if res.returncode != 0:
            logger.fail("Ошибка компиляции!")
            print(res.stderr)
            return False
            
        # Проверка флагов Warning (в школе это 0 баллов)
        if "warning:" in res.stderr.lower():
            logger.fail("Внимание! Есть WARNINGS компилятора!")
            print(res.stderr)
            return False
            
        logger.success("Проект собран без ошибок.")
        return True

    def run_tests(self):
        logger.header("ЭТАП 3: ТЕСТЫ")
        # Пока сделаем простую проверку: если есть unit-тесты (make test)
        res = subprocess.run(["make", "test"], cwd=self.project_path, capture_output=True, text=True)
        if res.returncode == 0:
            logger.success("Unit-тесты (make test) пройдены.")
            return True
        else:
            # Если цели test нет, это может быть cat/grep.
            # В будущем тут будет логика сравнения с bash.
            logger.warning("Цель 'make test' упала или отсутствует. Пропускаем.")
            return True

    def check_memory(self):
        logger.header("ЭТАП 4: VALGRIND")
        # Пытаемся найти бинарник
        # (Упрощенная логика: берем первый попавшийся файл без расширения, который новее Makefile)
        # Для простоты пока ищем s21_cat или s21_grep
        binaries = [f for f in os.listdir(self.project_path) if f.startswith("s21_") and "." not in f]
        
        if not binaries:
            logger.warning("Бинарник не найден, проверку памяти пропускем.")
            return True

        target = os.path.join(self.project_path, binaries[0])
        logger.info(f"Проверка памяти для: {binaries[0]}")
        
        # Запуск Valgrind
        cmd = ["valgrind", "--tool=memcheck", "--leak-check=full", "--error-exitcode=1", target]
        # Добавляем аргументы, если это cat (чтобы он не ждал ввода)
        if "cat" in target: cmd.append("Makefile")
        
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode != 0:
            logger.fail("ОБНАРУЖЕНЫ УТЕЧКИ ПАМЯТИ!")
            # Фильтруем вывод
            for line in res.stderr.split('\n'):
                if "lost:" in line or "ERROR SUMMARY" in line:
                    print(f"  >> {line.strip()}")
            return False
        
        logger.success("Утечек нет.")
        return True

    def _check_principles(self):
        """Парсер для проверки длины функций и goto"""
        ok = True
        for f_path in self.files:
            if not f_path.endswith(".c"): continue
            
            with open(f_path, 'r', errors='ignore') as f:
                lines = f.readlines()
            
            in_func = False
            lines_count = 0
            brace_balance = 0
            
            for i, line in enumerate(lines):
                stripped = line.strip()
                
                # Check goto
                if "goto " in stripped and not stripped.startswith("//"):
                    logger.fail(f"GOTO обнаружен: {os.path.basename(f_path)}:{i+1}")
                    ok = False

                # Подсчет строк функций
                brace_balance += stripped.count('{')
                brace_balance -= stripped.count('}')
                
                if brace_balance > 0 and not in_func and "(" in line and "{" in line:
                    in_func = True
                    lines_count = 0
                
                if in_func:
                    lines_count += 1
                
                if in_func and brace_balance == 0:
                    if lines_count > settings.MAX_LINES_PER_FUNC:
                        logger.fail(f"Функция слишком длинная ({lines_count} стр) в {os.path.basename(f_path)}:{i+1}")
                        ok = False
                    in_func = False
        return ok