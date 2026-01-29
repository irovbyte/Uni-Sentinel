
import sys
from config import settings

class Logger:
    def header(self, message):
        print(f"\n{settings.Colors.HEADER}{settings.Colors.BOLD}=== {message} ==={settings.Colors.ENDC}")

    def info(self, message):
        print(f"{settings.Colors.OKBLUE}[INFO]{settings.Colors.ENDC} {message}")

    def success(self, message):
        print(f"{settings.Colors.OKGREEN}[OK]{settings.Colors.ENDC} {message}")

    def warning(self, message):
        print(f"{settings.Colors.WARNING}[WARN]{settings.Colors.ENDC} {message}")

    def fail(self, message):
        print(f"{settings.Colors.FAIL}[FAIL]{settings.Colors.ENDC} {message}")

logger = Logger()