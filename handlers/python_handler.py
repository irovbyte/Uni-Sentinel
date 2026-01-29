import subprocess
import os
from handlers.base_handler import BaseHandler
from core.logger import logger

class PythonHandler(BaseHandler):
    def check_style(self):
        logger.header("ЭТАП 1: СТИЛЬ (PEP8)")
        try:
            # Flake8 - стандарт индустрии. Проверяем все .py файлы
            cmd = ["flake8"] + self.files
            # capture_output=True чтобы не спамить в консоль, если все ок
            res = subprocess.run(cmd, capture_output=True, text=True)
            
            if res.returncode != 0:
                logger.fail("Найдены нарушения PEP8:")
                print(res.stdout)
                return False
            
            logger.success("Код соответствует PEP8.")
            return True
        except FileNotFoundError:
            logger.warning("Flake8 не установлен. Выполни: pip install flake8")
            return True

    def build(self):
        logger.header("ЭТАП 2: СИНТАКСИС")
        all_ok = True
        for f in self.files:
            # Проверяем синтаксис без запуска
            res = subprocess.run(["python3", "-m", "py_compile", f], capture_output=True, text=True)
            if res.returncode != 0:
                logger.fail(f"Синтаксическая ошибка в {os.path.basename(f)}")
                print(res.stderr)
                all_ok = False
        
        if all_ok: logger.success("Синтаксис корректен.")
        return all_ok

    def run_tests(self):
        logger.header("ЭТАП 3: ТЕСТЫ")
        # Пытаемся запустить pytest
        try:
            res = subprocess.run(["pytest"], cwd=self.project_path, capture_output=True, text=True)
            if res.returncode == 0:
                logger.success("Все тесты pytest пройдены.")
            elif res.returncode == 5:
                logger.warning("Тесты не найдены (pytest вернул код 5).")
            else:
                logger.fail("Тесты упали.")
                # Выводим последние строки ошибки
                print(res.stdout[-500:]) 
                return False
        except FileNotFoundError:
            logger.warning("Pytest не установлен.")
        return True

    def check_memory(self):
        # В Python нет Valgrind, пропускаем
        return True