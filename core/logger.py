import os
import datetime

class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'

class UltraLogger:
    def __init__(self):
        self.log_file = os.path.join("S21_Ultra_Linter", "logs", "session.log")
        self.errors = 0
        self.warnings = 0
        # Очищаем лог при старте
        with open(self.log_file, 'w') as f:
            f.write(f"=== S21 SESSION START: {datetime.datetime.now()} ===\n")

    def _write_to_file(self, tag, message):
        """Пишет в файл без цветовых кодов"""
        timestamp = datetime.datetime.now().strftime("%H:%M:%S")
        with open(self.log_file, 'a') as f:
            f.write(f"[{timestamp}] [{tag}] {message}\n")

    def info(self, message):
        print(f"{Colors.OKBLUE}[INFO]{Colors.ENDC} {message}")
        self._write_to_file("INFO", message)

    def success(self, message):
        print(f"{Colors.OKGREEN}[OK]{Colors.ENDC} {message}")
        self._write_to_file("OK", message)

    def warning(self, message):
        self.warnings += 1
        print(f"{Colors.WARNING}[WARN]{Colors.ENDC} {message}")
        self._write_to_file("WARN", message)

    def fail(self, message):
        self.errors += 1
        print(f"{Colors.FAIL}[FAIL]{Colors.ENDC} {message}")
        self._write_to_file("FAIL", message)

    def header(self, message):
        print(f"\n{Colors.BOLD}{Colors.HEADER}=== {message} ==={Colors.ENDC}")
        self._write_to_file("SECTION", f"=== {message} ===")

    def print_summary(self):
        """Выводит итоговую статистику внизу, как ты просил"""
        print("\n" + "="*40)
        print(f"{Colors.BOLD}ИТОГОВЫЙ ОТЧЕТ:{Colors.ENDC}")
        if self.errors == 0 and self.warnings == 0:
            print(f"{Colors.OKGREEN}ВСЕ ЧИСТО! ТЫ КРАСАВЧИК! 😎{Colors.ENDC}")
            status = "SUCCESS"
        else:
            print(f"Ошибок (FAIL): {Colors.FAIL}{self.errors}{Colors.ENDC}")
            print(f"Предупреждений (WARN): {Colors.WARNING}{self.warnings}{Colors.ENDC}")
            print(f"{Colors.BOLD}Чекни подробности в файле: {self.log_file}{Colors.ENDC}")
            status = "HAS_ERRORS"
        
        self._write_to_file("SUMMARY", f"Status: {status}, Errors: {self.errors}, Warnings: {self.warnings}")
        print("="*40 + "\n")

# Создаем единственный экземпляр логгера, который будем импортировать везде
logger = UltraLogger()