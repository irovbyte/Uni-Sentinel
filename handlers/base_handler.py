class BaseHandler:
    def __init__(self, project_path, files):
        self.project_path = project_path
        self.files = files

    def check_style(self):
        """Проверка стиля кода (linter)"""
        raise NotImplementedError("Метод check_style должен быть реализован")

    def build(self):
        """Сборка проекта (компиляция)"""
        return True # По умолчанию успех (для Python не нужно)

    def run_tests(self):
        """Запуск тестов"""
        return True

    def check_memory(self):
        """Проверка утечек памяти"""
        return True