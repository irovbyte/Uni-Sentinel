# config/settings.py

VERSION = "1.0.0"
APP_NAME = "Uni-Sentinel"
REPO_URL = "https://github.com/irovbyte/Uni-Sentinel"


MAX_LINES_PER_FUNC = 50
MAX_NESTING_LEVEL = 4
REQUIRED_FLAGS = ["-Wall", "-Werror", "-Wextra"]
FORBIDDEN_KEYWORDS = ["goto"]


class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'