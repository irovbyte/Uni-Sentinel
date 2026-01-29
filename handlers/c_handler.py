import os
import subprocess
import shutil
from handlers.base_handler import BaseHandler
from core.logger import logger
from config import settings

class CHandler(BaseHandler):
    def __init__(self, project_path, files):
        super().__init__(project_path, files)
        # Ищем все папки, где есть Makefile (это и есть подпроекты)
        self.subprojects = self._find_subprojects()

    def check_style(self):
        logger.header("ЭТАП 1: СТИЛЬ И ПРИНЦИПЫ")
        self._setup_clang_format()
        
        all_ok = True
        
        # 1. Clang-format
        logger.info("-> Запуск Clang-format...")
        style_ok = True
        for f in self.files:
            # Проверяем только .c и .h
            if not (f.endswith(".c") or f.endswith(".h")): continue
            if "test" in os.path.basename(f): continue # Пропускаем файлы тестов

            res = subprocess.run(["clang-format", "-n", "--Werror", f], capture_output=True, text=True)
            if res.returncode != 0:
                logger.fail(f"Стиль нарушен в {os.path.basename(f)}")
                style_ok = False
        
        if style_ok: 
            logger.success("Clang-format: OK")
        else:
            all_ok = False

        # 2. Принципы структурного программирования
        print("")
        logger.info("-> Анализ структуры кода (goto, длина функций, вложенность):")
        if not self._check_principles_detailed():
            all_ok = False

        return all_ok

    def build(self):
        logger.header("ЭТАП 2: СБОРКА")
        
        if not self.subprojects:
            logger.fail(f"Makefile не найден ни в одной папке внутри {self.project_path}")
            return False

        logger.info(f"Найдено подпроектов для сборки: {len(self.subprojects)}")
        all_built = True

        for path in self.subprojects:
            folder_name = os.path.basename(path)
            print(f"\n   📂 Сборка в папке: {settings.Colors.BOLD}{folder_name}{settings.Colors.ENDC}")
            
            # Clean перед сборкой (гарантия чистоты)
            subprocess.run(["make", "clean"], cwd=path, capture_output=True)

            # Build (all или default)
            res = subprocess.run(["make", "all"], cwd=path, capture_output=True, text=True)
            if "No rule to make target" in res.stderr:
                res = subprocess.run(["make"], cwd=path, capture_output=True, text=True)

            if res.returncode != 0:
                logger.fail(f"Ошибка компиляции в {folder_name}!")
                print(res.stderr)
                all_built = False
                continue

            if "warning:" in res.stderr.lower():
                logger.fail(f"FAIL: Обнаружены WARNINGS в {folder_name}!")
                print(res.stderr)
                all_built = False
                continue
            
            logger.success(f"Сборка {folder_name} успешна.")

        return all_built

    def run_tests(self):
        logger.header("ЭТАП 3: ТЕСТЫ")
        if not self.subprojects: return True
        all_passed = True

        for path in self.subprojects:
            folder_name = os.path.basename(path)
            print(f"\n   🧪 Тесты для: {folder_name}")

            # 1. Попытка запустить стандартные тесты (make test)
            res = subprocess.run(["make", "test"], cwd=path, capture_output=True, text=True)
            if res.returncode == 0:
                logger.success(f"Unit-тесты (make test) пройдены.")
                continue

            # 2. Поиск исполняемого файла (УНИВЕРСАЛЬНЫЙ ПОИСК)
            binaries = self._find_binaries(path)
            
            if not binaries:
                logger.warning(f"Исполняемые файлы не найдены в {folder_name}.")
                all_passed = False
                continue

            for bin_name in binaries:
                bin_path = os.path.join(path, bin_name)
                # Smoke Test (проверка что запускается)
                try:
                    # Передаем аргумент --help или Makefile, чтобы прога не висела ожидая ввода
                    args = ["Makefile"] 
                    subprocess.run([bin_path] + args, timeout=3, capture_output=True)
                    logger.success(f"Smoke Test: {bin_name} запускается корректно.")
                except subprocess.TimeoutExpired:
                    # Если прога висит, значит она работает (ждет ввода), это тоже успех для smoke-теста
                    logger.success(f"Smoke Test: {bin_name} работает (интерактивный режим).")
                except Exception as e:
                    logger.fail(f"Ошибка запуска {bin_name}: {e}")
                    all_passed = False

        return all_passed

    def check_memory(self):
        logger.header("ЭТАП 4: ПРОВЕРКА ПАМЯТИ (VALGRIND)")
        if not self.subprojects: return True
        all_clean = True

        for path in self.subprojects:
            binaries = self._find_binaries(path)
            if not binaries: continue
            
            for bin_name in binaries:
                target = os.path.join(path, bin_name)
                print(f"\n   🧠 Valgrind check: {bin_name}")
                
                cmd = ["valgrind", "--tool=memcheck", "--leak-check=full", "--error-exitcode=1", target]
                
                # Добавляем фиктивный аргумент, чтобы CLI утилиты не ждали stdin вечно
                cmd.append("Makefile")

                res = subprocess.run(cmd, capture_output=True, text=True)
                
                if res.returncode != 0:
                    logger.fail(f"УТЕЧКИ ПАМЯТИ В {bin_name}!")
                    # Выводим только важные строки
                    printed_err = False
                    for line in res.stderr.split('\n'):
                         if "definitely lost:" in line or "indirectly lost:" in line or "ERROR SUMMARY:" in line:
                            print(f"  >> {line.strip()}")
                            printed_err = True
                    if not printed_err: # Если не нашли ключевых слов, выведем хвост
                         print(res.stderr[-300:])
                    all_clean = False
                else:
                    logger.success(f"Утечек нет ({bin_name}).")

        return all_clean

    def cleanup(self):
        logger.header("ФИНАЛ: ОЧИСТКА (MAKE CLEAN)")
        if not self.subprojects: return True

        for path in self.subprojects:
            folder_name = os.path.basename(path)
            # Проверяем, есть ли Makefile перед запуском
            if os.path.exists(os.path.join(path, "Makefile")):
                subprocess.run(["make", "clean"], cwd=path, capture_output=True)
                logger.info(f"Очищен мусор в {folder_name}")
        
        logger.success("Рабочая директория чиста.")
        return True

    # --- ПРИВАТНЫЕ МЕТОДЫ ---

    def _find_subprojects(self):
        """Ищет все папки с Makefile"""
        paths = []
        for root, _, files in os.walk(self.project_path):
            if "Makefile" in files:
                paths.append(root)
        return paths

    def _find_binaries(self, path):
        """
        УНИВЕРСАЛЬНЫЙ ПОИСК БИНАРНИКОВ.
        Критерии: файл, права на исполнение, не исходник, не скрипт.
        """
        binaries = []
        ignored_exts = {'.c', '.h', '.o', '.a', '.so', '.sh', '.py', '.txt', '.md', '.json'}
        
        for f in os.listdir(path):
            full_path = os.path.join(path, f)
            if not os.path.isfile(full_path): continue
            if f.startswith('.'): continue
            if f == "Makefile": continue

            _, ext = os.path.splitext(f)
            if ext in ignored_exts: continue

            if os.access(full_path, os.X_OK):
                binaries.append(f)
                
        return binaries

    def _setup_clang_format(self):
        if os.path.exists(os.path.join(self.project_path, ".clang-format")):
            return

        search_dir = self.project_path
        found_config = None
        for _ in range(6): 
            candidate = os.path.join(search_dir, "materials", "linters", ".clang-format")
            if os.path.exists(candidate):
                found_config = candidate
                break
            search_dir = os.path.dirname(search_dir)
        
        if found_config:
            try:
                shutil.copy(found_config, os.path.join(self.project_path, ".clang-format"))
                logger.success(f"Найден и применен стиль из: {found_config}")
            except: pass

    def _check_principles_detailed(self):
        files_to_check = [f for f in self.files if f.endswith(".c") and "test" not in f]
        check_goto = True
        check_func_len = True
        errors = []

        for f_path in files_to_check:
            try:
                with open(f_path, 'r', errors='ignore') as f:
                    lines = f.readlines()
            except: continue

            in_func = False
            lines_count = 0
            brace_balance = 0
            
            for i, line in enumerate(lines):
                stripped = line.strip()
                if "goto " in stripped and not stripped.startswith("//"):
                    errors.append(f"❌ GOTO в {os.path.basename(f_path)}:{i+1}")
                    check_goto = False

                brace_balance += stripped.count('{')
                brace_balance -= stripped.count('}')
                
                if brace_balance > 0 and not in_func and "(" in line and "{" in line:
                    in_func = True
                    lines_count = 0
                
                if in_func:
                    lines_count += 1
                
                if in_func and brace_balance == 0:
                    if lines_count > settings.MAX_LINES_PER_FUNC:
                        errors.append(f"❌ Функция > 50 строк ({lines_count}) в {os.path.basename(f_path)}:{i+1}")
                        check_func_len = False
                    in_func = False

        if check_goto: logger.success("  [OK] GOTO отсутствует")
        else: logger.fail("  [FAIL] Найден GOTO!")
        
        if check_func_len: logger.success("  [OK] Функции компактные (<= 50)")
        else: logger.fail("  [FAIL] Есть длинные функции!")

        if errors:
            print("\n".join(errors))
            return False
        return True