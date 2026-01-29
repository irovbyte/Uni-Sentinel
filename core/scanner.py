import os
import re
import sys
import subprocess
import shutil

# Хак для импорта
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from config import settings
from core.logger import logger

class Scanner:
    def __init__(self, root_path="."):
        self.root_path = os.path.abspath(root_path)

    def check_git_branch(self):
        """Проверяет, что мы находимся в ветке develop"""
        logger.header("ЭТАП 0: ПРОВЕРКА GIT")
        try:
            res = subprocess.run(["git", "branch", "--show-current"], 
                                 cwd=self.root_path, capture_output=True, text=True)
            branch = res.stdout.strip()
            
            if branch == "develop":
                logger.success(f"Текущая ветка: {branch}")
                return True
            else:
                logger.warning(f"Ты находишься в ветке '{branch}', а школа требует 'develop'!")
                return False # Можно вернуть True, если не хочешь блокировать работу
        except FileNotFoundError:
            logger.warning("Git не установлен или это не репозиторий.")
            return True

    def setup_linters(self):
        """Ищет .clang-format в materials и копирует в корень src"""
        logger.info("Настройка конфигов линтера...")
        
        # Ищем папку materials (поднимаемся вверх, если надо)
        materials_config = None
        for root, dirs, files in os.walk(self.root_path):
            if "materials" in root and ".clang-format" in files:
                materials_config = os.path.join(root, ".clang-format")
                break
        
        # Если не нашли рекурсивно, проверим жесткий путь (для надежности)
        if not materials_config:
            candidate = os.path.join(self.root_path, "materials", "linters", ".clang-format")
            if os.path.exists(candidate):
                materials_config = candidate

        if materials_config:
            # Копируем в папку src (если она есть) или в корень
            dest_dir = os.path.join(self.root_path, "src")
            if not os.path.exists(dest_dir):
                dest_dir = self.root_path
            
            dest_file = os.path.join(dest_dir, ".clang-format")
            
            try:
                shutil.copy(materials_config, dest_file)
                logger.success(f"Конфиг скопирован: {materials_config} -> {dest_file}")
            except Exception as e:
                logger.fail(f"Ошибка копирования конфига: {e}")
        else:
            logger.warning("Не удалось найти materials/linters/.clang-format. Будет использован дефолтный стиль.")

    def scan(self):
        logger.header(f"ЭТАП 1: УНИВЕРСАЛЬНОЕ СКАНИРОВАНИЕ")
        found_projects = []
        exclude_dirs = {".git", ".vscode", "S21_Ultra_Linter", "materials", "build", "linters"}

        for root, dirs, files in os.walk(self.root_path):
            dirs[:] = [d for d in dirs if d not in exclude_dirs]

            if "Makefile" in files:
                proj = self._analyze_dir(root, files)
                if proj:
                    found_projects.append(proj)
        
        return found_projects

    def _analyze_dir(self, path, files):
        # 1. Ищем .c и .h файлы
        c_files = [os.path.join(path, f) for f in files if f.endswith(".c")]
        h_files = [os.path.join(path, f) for f in files if f.endswith(".h")]
        
        if not c_files: 
            return None 

        # 2. Определяем тип: CLI или LIB
        project_type = "LIB" 
        has_main = False
        
        for cf in c_files:
            try:
                with open(cf, 'r', errors='ignore') as f:
                    if re.search(r'\bint\s+main\s*\(', f.read()):
                        has_main = True
                        break
            except: pass
        
        if has_main:
            project_type = "CLI"

        # 3. Угадываем имя бинарника
        makefile_path = os.path.join(path, "Makefile")
        binary_name = os.path.basename(path) # Дефолт
        
        # Читаем Makefile, чтобы проверить флаги и найти имя цели
        makefile_ok = True
        try:
            with open(makefile_path, 'r') as f:
                content = f.read()
                # Имя цели
                match = re.search(r'^(s21_\w+):', content, re.MULTILINE)
                if match:
                    binary_name = match.group(1)
                
                # Проверка флагов (Обязательно для школы!)
                flat_content = content.replace("\\\n", " ")
                for flag in settings.REQUIRED_FLAGS:
                    if flag not in flat_content:
                        logger.fail(f"Makefile в {os.path.basename(path)}: Нет флага {flag}!")
                        makefile_ok = False

        except: pass

        if makefile_ok:
            logger.info(f"Найден проект: {binary_name} | Тип: {project_type}")
            return {
                "name": binary_name,
                "path": path,
                "type": project_type,
                "files": c_files + h_files
            }
        else:
            return None