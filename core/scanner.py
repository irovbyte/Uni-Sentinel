import os
from core.logger import logger
# Импортируем хендлеры (плагины)
from handlers.c_handler import CHandler
from handlers.python_handler import PythonHandler

class Scanner:
    def __init__(self, root_path):
        self.root_path = os.path.abspath(root_path)

    def detect_handler(self):
        """Сканирует папку и возвращает подходящий Handler"""
        c_count = 0
        py_count = 0
        project_files = []

        # 1. Бежим по папке
        exclude = {".git", ".vscode", ".uni-sentinel", "materials"}
        for root, dirs, files in os.walk(self.root_path):
            dirs[:] = [d for d in dirs if d not in exclude]
            
            for f in files:
                full_path = os.path.join(root, f)
                if f.endswith(".c"):
                    c_count += 1
                    project_files.append(full_path)
                elif f.endswith(".py"):
                    py_count += 1
                    project_files.append(full_path)

        # 2. Выбираем стратегию
        if c_count > 0 and c_count >= py_count:
            logger.info(f"Обнаружен C-проект ({c_count} файлов).")
            return CHandler(self.root_path, project_files)
        
        elif py_count > 0:
            logger.info(f"Обнаружен Python-проект ({py_count} файлов).")
            return PythonHandler(self.root_path, project_files)
            
        else:
            logger.warning("Подходящие файлы (.c, .py) не найдены.")
            return None