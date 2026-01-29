# --- ПРАВИЛА SCHOOL 21 ---

# Структурное программирование
MAX_LINES_PER_FUNC = 50
MAX_NESTING_LEVEL = 4
MAX_FUNCS_IN_FILE = 5  # Обычно 5 функций на файл

# Запрещенные слова
FORBIDDEN_KEYWORDS = [
    "goto",      # Принцип Дейкстры №1
    "printf",    # Часто запрещен в финальных проектах (нужен write), но для debug можно оставить
]

# Компиляция
REQUIRED_FLAGS = ["-Wall", "-Werror", "-Wextra", "-std=c11"]
REQUIRED_TARGETS = ["all", "clean", "fclean", "re"]

# Тесты
MEMORY_CHECK_TOOL = "valgrind" # или "leaks" для Mac