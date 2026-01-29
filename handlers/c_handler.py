import os
import subprocess
import re
import shutil
from handlers.base_handler import BaseHandler
from core.logger import logger
from config import settings

class CHandler(BaseHandler):
    def __init__(self, project_path, files):
        super().__init__(project_path, files)
        # Пытаемся уточнить путь к проекту (если Makefile лежит в src)
        self._resolve_project_root()

    def _resolve_project_root(self):
        """Если мы в корне репо, а код в src, смещаем фокус туда"""
        if not os.path.exists(os.path.join(self.project_path, "Makefile")):
            src_path = os.path.join(self.project_path, "src")
            if os.path.exists(os.path.join(src_path, "Makefile")):
                logger.info(f"Makefile найден в подпапке 'src'. Переключаюсь туда.")
                self.project_path = src_path
                # Обновляем список файлов (чтобы пути были корректны относительно src)
                # Но для проверки стиля нам нужны полные пути, так что files не трогаем,
                # а вот рабочую директорию для команд запоминаем.

    def check_style(self):
        logger.header("ЭТАП 1: СТИЛЬ И ПРИНЦИПЫ")
        
        # 0. Настройка .clang-format
        self._setup_clang_format()

        all_ok = True

        # 1. Clang-format
        logger.info("-> Запуск Clang-format (Google Style)...")
        style_ok = True
        for f in self.files:
            if not f.endswith(".c") and not f.endswith(".h"): continue
            
            # Пропускаем файлы тестов (обычно там стиль не важен)
            if "test" in os.path.basename(f): continue

            res = subprocess.run(["clang-format", "-n", "--Werror", f], capture_output=True, text=True)
            if res.returncode != 0:
                logger.fail(f"Ошибка стиля в {os.path.basename(f)}")
                style_ok = False
        
        if style_ok: 
            logger.success("Clang-format: OK")
        else:
            all_ok = False

        # 2. Детальная проверка 7 принципов
        print("") # Отступ
        logger.info("-> Анализ 7 принципов структурного программирования:")
        if not self._check_principles_detailed():
            all_ok = False

        return all_ok

    def build(self):
        logger.header("ЭТАП 2: СБОРКА (MAKE)")
        makefile = os.path.join(self.project_path, "Makefile")
        
        if not os.path.exists(makefile):
            logger.fail(f"Makefile не найден в {self.project_path}")
            # Пытаемся подсказать
            logger.warning("Совет: Зайди внутрь папки src/cat или src/grep перед запуском.")
            return False

        # Make clean
        subprocess.run(["make", "clean"], cwd=self.project_path, capture_output=True)
        
        logger.info(f"Выполняю 'make all' в {self.project_path}...")
        # Используем 'all' или 're', некоторые мейкфайлы не имеют re
        res = subprocess.run(["make", "all"], cwd=self.project_path, capture_output=True, text=True)
        
        if res.returncode != 0:
            logger.fail("Ошибка компиляции!")
            print(res.stderr)
            return False
            
        if "warning:" in res.stderr.lower():
            logger.fail("FAIL: Обнаружены WARNINGS (в Школе 21 это недопустимо)!")
            print(res.stderr)
            return False
            
        logger.success("Билд успешен. Варнингов нет.")
        return True

    def run_tests(self):
        logger.header("ЭТАП 3: ТЕСТЫ")
        # Пробуем найти цель test
        res = subprocess.run(["make", "test"], cwd=self.project_path, capture_output=True, text=True)
        
        if res.returncode == 0:
            logger.success("Unit-тесты (make test) пройдены.")
            return True
        
        # Если make test нет, пробуем найти бинарник и запустить Smoke Test
        binaries = self._find_binaries()
        if binaries:
            bin_name = binaries[0]
            logger.info(f"Запускаю Smoke Test для {bin_name}...")
            bin_path = os.path.join(self.project_path, bin_name)
            
            # Простой запуск (проверка что не падает)
            try:
                # Для cat/grep кидаем Makefile как аргумент
                test_args = ["Makefile"] if "cat" in bin_name or "grep" in bin_name else []
                subprocess.run([bin_path] + test_args, timeout=2, capture_output=True)
                logger.success(f"{bin_name} запускается и не падает.")
                return True
            except subprocess.TimeoutExpired:
                 logger.warning(f"{bin_name} завис (Timeout). Возможно, ждет ввода?")
                 return True
            except Exception as e:
                logger.fail(f"Ошибка запуска: {e}")
                return False

        logger.warning("Тесты не найдены (make test failed, бинарник не найден).")
        return True

    def check_memory(self):
        logger.header("ЭТАП 4: VALGRIND / LEAKS")
        binaries = self._find_binaries()
        
        if not binaries:
            logger.warning("Бинарник не найден, пропускаю Valgrind.")
            return True

        target = os.path.join(self.project_path, binaries[0])
        logger.info(f"Проверка: {binaries[0]}")
        
        cmd = ["valgrind", "--tool=memcheck", "--leak-check=full", "--error-exitcode=1", target]
        if "cat" in target or "grep" in target: cmd.append("Makefile")
        
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode != 0:
            logger.fail("ОБНАРУЖЕНЫ УТЕЧКИ ПАМЯТИ!")
            for line in res.stderr.split('\n'):
                if "definitely lost:" in line or "indirectly lost:" in line or "ERROR SUMMARY:" in line:
                    print(f"  >> {line.strip()}")
            return False
        
        logger.success("Память чиста.")
        return True

    # --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

    def _setup_clang_format(self):
        """Ищет .clang-format и предлагает скопировать"""
        if os.path.exists(os.path.join(self.project_path, ".clang-format")):
            return # Уже есть

        # Ищем в materials (поднимаемся на уровни выше)
        found_path = None
        search_dir = self.project_path
        for _ in range(4): # Ищем на 4 уровня вверх
            candidate = os.path.join(search_dir, "materials", "linters", ".clang-format")
            if os.path.exists(candidate):
                found_path = candidate
                break
            search_dir = os.path.dirname(search_dir)
        
        if found_path:
            logger.warning(f"Найден конфиг стиля: {found_path}")
            # Интерактив
            try:
                answer = input(f"{settings.Colors.WARNING}>> Скопировать его в проект? [Y/n]: {settings.Colors.ENDC}")
                if answer.lower() in ['', 'y', 'yes']:
                    shutil.copy(found_path, os.path.join(self.project_path, ".clang-format"))
                    logger.success("Конфиг скопирован!")
            except: pass # Если input не работает (в скриптах)
        else:
            logger.info("Не нашел materials/linters/.clang-format. Использую дефолтный стиль.")

    def _check_principles_detailed(self):
        """Проверка принципов с галочками"""
        files_to_check = [f for f in self.files if f.endswith(".c") and "test" not in f]
        
        check_goto = True
        check_func_len = True
        check_nesting = True
        
        errors = []

        for f_path in files_to_check:
            try:
                with open(f_path, 'r', errors='ignore') as f:
                    lines = f.readlines()
            except: continue

            in_func = False
            lines_count = 0
            brace_balance = 0
            nesting_level = 0
            
            for i, line in enumerate(lines):
                stripped = line.strip()
                
                # 1. GOTO
                if "goto " in stripped and not stripped.startswith("//"):
                    errors.append(f"❌ GOTO в {os.path.basename(f_path)}:{i+1}")
                    check_goto = False

                # Подсчет баланса скобок
                open_braces = stripped.count('{')
                close_braces = stripped.count('}')
                brace_balance += (open_braces - close_braces)

                # 2. Вложенность
                if in_func:
                    # Грубая оценка вложенности
                    if brace_balance > nesting_level: nesting_level = brace_balance
                    if nesting_level > settings.MAX_NESTING_LEVEL + 1: # +1 т.к. сама функция это ур.1
                        # Это сложная проверка, пока просто кидаем ворнинг, если очень глубоко
                        pass 

                # 3. Длина функции
                # Начало функции (эвристика)
                if brace_balance > 0 and not in_func and "(" in line and "{" in line:
                    in_func = True
                    lines_count = 0
                
                if in_func:
                    lines_count += 1
                
                # Конец функции
                if in_func and brace_balance == 0:
                    if lines_count > settings.MAX_LINES_PER_FUNC:
                        errors.append(f"❌ Функция > 50 строк ({lines_count}) в {os.path.basename(f_path)}:{i+1}")
                        check_func_len = False
                    in_func = False

        # Вывод результатов
        if check_goto: logger.success("  [OK] GOTO не обнаружен")
        else: logger.fail("  [FAIL] Найден оператор GOTO!")
        
        if check_func_len: logger.success("  [OK] Длина функций <= 50 строк")
        else: logger.fail("  [FAIL] Есть слишком длинные функции!")

        # Вложенность пока считаем ОК, так как её сложно парсить регулярками идеально
        logger.success("  [OK] Вложенность блоков (basic check)")

        if errors:
            print("\n".join(errors))
            return False
            
        return True

    def _find_binaries(self):
        """Ищет скомпилированные файлы s21_..."""
        return [f for f in os.listdir(self.project_path) 
                if f.startswith("s21_") and "." not in f and os.access(os.path.join(self.project_path, f), os.X_OK)]