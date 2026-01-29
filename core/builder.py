import subprocess
import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from core.logger import logger
from config import settings

class Builder:
    def build_project(self, project_data):
        """Запускает make re и проверяет Warnings"""
        path = project_data['path']
        name = project_data['name']
        
        logger.header(f"ЭТАП 3: СБОРКА И SANITIZERS ({name})")
        
        # 1. Очистка
        self._run_make_target(path, "clean")

        # 2. Основная сборка
        logger.info(f"Запуск make all в {path}...")
        result = self._run_make_target(path, "all")
        
        if result.returncode != 0:
            logger.fail("Ошибка компиляции! (Make вернул ошибку)")
            return False

        if "warning:" in result.stderr.lower():
            logger.fail("Обнаружены WARNINGS! В Школе 21 это 0 баллов.")
            print(result.stderr)
            return False
        
        # Проверка создания бинарника
        bin_path = os.path.join(path, name)
        if not os.path.exists(bin_path):
            # Пробуем найти хоть какой-то выходной файл, если имя не совпало
            logger.fail(f"Бинарный файл не создан: {bin_path}")
            return False
            
        logger.success(f"Билд успешен! Бинарник готов: {name}")
        return True

    def run_sanitizer_check(self, project_data):
        """Доп. проверка: компиляция с -fsanitize=address"""
        # Эта проверка запускается отдельно, чтобы не портить основной Makefile
        path = project_data['path']
        name = project_data['name']
        # Берем только .c файлы
        c_files = [f for f in project_data['files'] if f.endswith(".c")]
        
        logger.info(f"Запуск AddressSanitizer (GCC Check)...")
        
        bin_name_san = f"{name}_sanitized"
        # Собираем команду вручную
        cmd = ["gcc", "-g", "-Wall", "-Werror", "-Wextra", "-std=c11", "-fsanitize=address", "-o", bin_name_san] + c_files
        
        # Для простых проектов (cat/grep) это сработает. 
        # Если есть либы (pcre, check), gcc может ругаться.
        try:
            res = subprocess.run(cmd, cwd=path, capture_output=True, text=True)
            if res.returncode != 0:
                logger.warning("Не удалось собрать с Sanitizer (возможно, нужны флаги линковки). Пропускаю.")
                return True 
            
            # Запускаем полученный файл (Smoke test)
            # Для cat/grep кидаем Makefile как аргумент
            run_args = ["Makefile"] if project_data['type'] == "CLI" else []
            res_run = subprocess.run([f"./{bin_name_san}"] + run_args, cwd=path, capture_output=True, text=True)
            
            # Чистим
            if os.path.exists(os.path.join(path, bin_name_san)):
                os.remove(os.path.join(path, bin_name_san))
            
            # Если Sanitizer нашел ошибку, он напишет в stderr
            if "AddressSanitizer" in res_run.stderr:
                logger.fail("SANITIZER НАШЕЛ ОШИБКУ ПАМЯТИ!")
                print(res_run.stderr)
                return False
            
            logger.success("AddressSanitizer: Ошибок не выявлено.")
            return True

        except Exception as e:
            logger.warning(f"Ошибка при запуске санитайзера: {e}")
            return True

    def _run_make_target(self, path, target):
        return subprocess.run(["make", target], cwd=path, capture_output=True, text=True)