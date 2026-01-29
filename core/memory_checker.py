import subprocess
import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from core.logger import logger
from config import settings

class MemoryChecker:
    def check_memory(self, project_data):
        name = project_data['name']
        path = project_data['path']
        bin_path = os.path.join(path, name)
        
        logger.header(f"ЭТАП 5: VALGRIND / LEAKS ({name})")
        
        # Проверяем, стоит ли валгринд
        tool = settings.MEMORY_CHECK_TOOL
        try:
            subprocess.run([tool, "--version"], capture_output=True)
        except:
            logger.warning(f"{tool} не установлен. Пропускаю проверку памяти.")
            return True

        # Берем простой тест для проверки памяти (например, чтение Makefile)
        # В будущем можно прогнать все кейсы
        test_args = []
        if "cat" in name:
            test_args = ["-benst", "Makefile"]
        elif "grep" in name:
            test_args = ["-iv", "int", "s21_grep.c"]
        else:
            test_args = ["Makefile"] # Дефолт

        cmd = [tool, "--tool=memcheck", "--leak-check=full", "--error-exitcode=1", bin_path] + test_args
        
        logger.info(f"Запуск: {' '.join(cmd)}")
        
        # Valgrind пишет ошибки в stderr
        result = subprocess.run(cmd, capture_output=True, text=True)
        
        # Если код возврата != 0 (из-за --error-exitcode=1), значит были утечки
        if result.returncode != 0:
            logger.fail(f"ОБНАРУЖЕНЫ УТЕЧКИ ПАМЯТИ!")
            # Фильтруем вывод, чтобы показать самое важное
            for line in result.stderr.split('\n'):
                if "definitely lost:" in line or "indirectly lost:" in line or "ERROR SUMMARY:" in line:
                    print(f"{logger.Colors.FAIL}  >> {line.strip()}{logger.Colors.ENDC}")
            return False
        
        logger.success("Утечек памяти не обнаружено. Память чиста!")
        return True