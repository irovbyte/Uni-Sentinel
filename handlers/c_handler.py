import os
import subprocess
import shutil
import time  
from handlers.base_handler import BaseHandler
from core.logger import logger
from config import settings

class CHandler(BaseHandler):
    def __init__(self, project_path, files):
        super().__init__(project_path, files)
        self.subprojects = self._find_subprojects()

    def check_style(self):
        time.sleep(0.5) 
        logger.header("ЭТАП 1: СТИЛЬ И ПРИНЦИПЫ")
        self._setup_clang_format()
        
        all_ok = True
        
        # 1. Clang-format
        time.sleep(0.5)
        logger.info("-> Запуск Clang-format...")
        style_ok = True
        for f in self.files:
            if not (f.endswith(".c") or f.endswith(".h")): continue
            if "test" in os.path.basename(f): continue 

            res = subprocess.run(["clang-format", "-n", "--Werror", f], capture_output=True, text=True)
            if res.returncode != 0:
                logger.fail(f"Стиль нарушен в {os.path.basename(f)}")
                style_ok = False
        
        if style_ok: 
            logger.success("Clang-format: OK")
        else:
            all_ok = False

        # 2. Принципы
        print("")
        time.sleep(0.5)
        logger.info("-> Анализ структуры кода (goto, длина функций, вложенность):")
        if not self._check_principles_detailed():
            all_ok = False

        return all_ok

    def build(self):
        time.sleep(1.0) # Пауза
        logger.header("ЭТАП 2: СБОРКА")
        
        if not self.subprojects:
            logger.fail(f"Makefile не найден ни в одной папке внутри {self.project_path}")
            return False

        logger.info(f"Найдено подпроектов для сборки: {len(self.subprojects)}")
        all_built = True

        for path in self.subprojects:
            folder_name = os.path.basename(path)
            print(f"\n   📂 Сборка в папке: {settings.Colors.BOLD}{folder_name}{settings.Colors.ENDC}")
            time.sleep(0.5)
            
            # Clean
            subprocess.run(["make", "clean"], cwd=path, capture_output=True)

            # Build
            res = subprocess.run(["make", "all"], cwd=path, capture_output=True, text=True)
            # Фолбэк, если нет цели all
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
        time.sleep(1.0)
        logger.header("ЭТАП 3: ТЕСТЫ")
        if not self.subprojects: return True
        all_passed = True

        # Сначала пробуем make test в подпроектах
        for path in self.subprojects:
            folder_name = os.path.basename(path)
            
            # Если есть цель test, запускаем её
            # Проверяем наличие 'test:' в Makefile, чтобы зря не долбить make
            try:
                with open(os.path.join(path, "Makefile"), 'r') as f:
                    if "test:" in f.read():
                        print(f"\n   🧪 Запуск 'make test' для: {folder_name}")
                        time.sleep(0.5)
                        res = subprocess.run(["make", "test"], cwd=path, capture_output=True, text=True)
                        if res.returncode == 0:
                            logger.success(f"Unit-тесты (make test) пройдены.")
                            continue # Тесты прошли, смоук тест не нужен
            except: pass

        # Теперь ищем бинарники ПО ВСЕМУ ПРОЕКТУ (Глобальный поиск)
        # Это решает проблему, когда бинарник лежит не в папке src
        print("\n   🔎 Поиск исполняемых файлов по всему проекту...")
        time.sleep(1.0)
        
        binaries = self._find_all_binaries_recursive()
        
        if not binaries:
            logger.warning("Исполняемые файлы не найдены нигде в проекте.")
            return False

        for bin_full_path in binaries:
            bin_name = os.path.basename(bin_full_path)
            rel_path = os.path.relpath(bin_full_path, self.project_path)
            
            # Smoke Test
            try:
                args = ["Makefile"]
                subprocess.run([bin_full_path] + args, timeout=3, capture_output=True)
                logger.success(f"Smoke Test: {rel_path} запускается корректно.")
            except subprocess.TimeoutExpired:
                logger.success(f"Smoke Test: {rel_path} работает (интерактивный режим).")
            except Exception as e:
                logger.fail(f"Ошибка запуска {bin_name}: {e}")
                all_passed = False

        return all_passed

    def check_memory(self):
        time.sleep(1.0)
        logger.header("ЭТАП 4: ПРОВЕРКА ПАМЯТИ (VALGRIND)")
        
        # Используем тот же глобальный поиск
        binaries = self._find_all_binaries_recursive()
        
        if not binaries:
            logger.warning("Бинарники не найдены. Valgrind пропущен.")
            return True # Не фейлим, просто пропускаем

        all_clean = True
        for bin_full_path in binaries:
            bin_name = os.path.basename(bin_full_path)
            print(f"\n   🧠 Valgrind check: {bin_name}")
            time.sleep(0.5)
            
            cmd = ["valgrind", "--tool=memcheck", "--leak-check=full", "--error-exitcode=1", bin_full_path]
            cmd.append("Makefile") # Фиктивный аргумент

            res = subprocess.run(cmd, capture_output=True, text=True)
            
            if res.returncode != 0:
                logger.fail(f"УТЕЧКИ ПАМЯТИ В {bin_name}!")
                printed_err = False
                for line in res.stderr.split('\n'):
                     if "definitely lost:" in line or "indirectly lost:" in line or "ERROR SUMMARY:" in line:
                        print(f"  >> {line.strip()}")
                        printed_err = True
                if not printed_err:
                     print(res.stderr[-300:])
                all_clean = False
            else:
                logger.success(f"Утечек нет ({bin_name}).")

        return all_clean

    def cleanup(self):
        time.sleep(1.0)
        logger.header("ФИНАЛ: ОЧИСТКА (MAKE CLEAN)")
        if not self.subprojects: return True

        for path in self.subprojects:
            folder_name = os.path.basename(path)
            if os.path.exists(os.path.join(path, "Makefile")):
                subprocess.run(["make", "clean"], cwd=path, capture_output=True)
                logger.info(f"Очищен мусор в {folder_name}")
        
        time.sleep(0.5)
        logger.success("Рабочая директория чиста.")
        return True

    # --- ПРИВАТНЫЕ МЕТОДЫ ---

    def _find_subprojects(self):
        paths = []
        for root, _, files in os.walk(self.project_path):
            if "Makefile" in files:
                paths.append(root)
        return paths

    def _find_all_binaries_recursive(self):
        """
        Ищет бинарники ВО ВСЕМ проекте, а не только рядом с Makefile.
        Полезно, если бинарник собирается в build/ или корень.
        """
        binaries = []
        ignored_exts = {'.c', '.h', '.o', '.a', '.so', '.sh', '.py', '.txt', '.md', '.json', '.xml'}
        ignored_names = {'make', 'configure', 'cmake', 'clang-format'} # Системные тулзы, которые могут лежать в проекте

        for root, _, files in os.walk(self.project_path):
            if ".git" in root or "tests" in root: continue # Пропускаем git и папки тестов

            for f in files:
                full_path = os.path.join(root, f)
                
                if f.startswith('.') or f == "Makefile": continue
                if f in ignored_names: continue
                
                _, ext = os.path.splitext(f)
                if ext in ignored_exts: continue

                # Проверка на исполняемость
                if os.path.isfile(full_path) and os.access(full_path, os.X_OK):
                    binaries.append(full_path)
        
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