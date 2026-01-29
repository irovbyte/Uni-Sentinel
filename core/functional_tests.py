import subprocess
import os
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from core.logger import logger

class FunctionalTester:
    def run_tests(self, project_data):
        p_type = project_data['type']
        name = project_data['name']
        path = project_data['path']
        
        logger.header(f"ЭТАП 4: ФУНКЦИОНАЛЬНЫЕ ТЕСТЫ ({p_type})")

        if p_type == "LIB":
            return self._test_library(path)
        else:
            return self._test_cli(path, name)

    def _test_library(self, path):
        """Тестирование библиотеки (s21_string, s21_math) через make test"""
        logger.info("Обнаружена библиотека. Запускаю Unit-тесты (make test)...")
        # Проверяем, есть ли цель test в Makefile
        # (Упрощенно пробуем запустить)
        res = subprocess.run(["make", "test"], cwd=path, capture_output=True, text=True)
        
        if res.returncode != 0:
            logger.fail("Unit-тесты упали или цель 'test' отсутствует!")
            # print(res.stderr) # Раскомментить для дебага
            return False
        
        # Часто в unit-тестах успех - это отсутствие слова FAILED в выводе
        if "FAILED" in res.stdout or "Failure" in res.stdout:
            logger.fail("Unit-тесты сообщили об ошибке.")
            return False
            
        logger.success("Unit-тесты пройдены успешно!")
        return True

    def _test_cli(self, path, binary_name):
        """Тестирование утилиты (cat, grep) через файл с аргументами"""
        bin_path = os.path.join(path, binary_name)
        tests_file = os.path.join(path, "tests.txt") # Ищем файл с тестами
        
        if not os.path.exists(bin_path):
            logger.warning(f"Бинарник {binary_name} не найден. Сначала сбилди.")
            return False

        args_list = []
        if os.path.exists(tests_file):
            logger.info(f"Найден файл с тестами: {tests_file}")
            with open(tests_file, 'r') as f:
                # Читаем строки, игнорируем пустые и комменты
                args_list = [line.strip() for line in f if line.strip() and not line.startswith("#")]
        else:
            logger.warning("Файл tests.txt не найден. Запускаю Smoke Test (без аргументов).")
            # Для универсальности создадим тест без аргументов или с --help
            args_list = [""] 

        passed_count = 0
        for args in args_list:
            # Разбиваем строку аргументов на список (как в терминале)
            cmd_args = args.split()
            if self._run_compare(bin_path, cmd_args, binary_name):
                passed_count += 1
        
        if passed_count == len(args_list):
            logger.success(f"Все тесты ({passed_count}) пройдены!")
            return True
        else:
            logger.fail(f"Пройдено {passed_count} из {len(args_list)}")
            return False

    def _run_compare(self, my_bin_path, args, bin_name):
        """Сравнивает вывод с системной утилитой"""
        # Эвристика: системная утилита обычно называется так же, но без s21_
        sys_bin = bin_name.replace("s21_", "")
        
        # Если системной утилиты нет (например, s21_smart_calc), сравнение невозможно
        # Проверяем наличие системной команды через which
        if subprocess.run(["which", sys_bin], capture_output=True).returncode != 0:
            # Если не с чем сравнивать, просто запускаем и смотрим, не упадет ли
            logger.info(f"Системная утилита {sys_bin} не найдена. Просто запускаю код...")
            res = subprocess.run([my_bin_path] + args, capture_output=True, text=True)
            if res.returncode < 0: # Отрицательный код значит сигнал (Segfault)
                logger.fail(f"Крах программы (Segfault) на аргументах: {' '.join(args)}")
                return False
            return True

        # Сравнение с системной
        sys_cmd = [sys_bin] + args
        my_cmd = [my_bin_path] + args
        
        sys_res = subprocess.run(sys_cmd, capture_output=True, text=True)
        my_res = subprocess.run(my_cmd, capture_output=True, text=True)

        if sys_res.stdout != my_res.stdout:
            logger.fail(f"Несовпадение вывода! Аргументы: {' '.join(args)}")
            return False
        
        return True