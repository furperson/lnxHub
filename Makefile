
CS_PROJ_DIR = ArchControl


C_LIB = libinterop.so

C_SRC = interop.c

# --- Команды ---

.PHONY: all build run clean


all: build

# Собрать проект: сначала компилируется С, затем .NET, затем библиотека копируется
build: $(C_LIB)
	@echo "--- Сборка проекта .NET ---"
	dotnet build $(CS_PROJ_DIR)
	@echo "--- Копирование библиотеки ---"
	@# Динамический поиск папки с результатами сборки .NET
	@OUTPUT_DIR=$$(find $(CS_PROJ_DIR)/bin/Debug -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' | sort -n | tail -1 | cut -d' ' -f2-); \
	if [ -n "$$OUTPUT_DIR" ]; then \
		cp $(C_LIB) $$OUTPUT_DIR/$(C_LIB); \
		echo "Библиотека $(C_LIB) скопирована в $$OUTPUT_DIR"; \
	else \
		echo "Предупреждение: не удалось найти папку для копирования библиотеки."; \
	fi

# Компиляция C-библиотеки
$(C_LIB): $(C_SRC)
	@echo "--- Компиляция библиотеки C ---"
	gcc -shared -fPIC -o $@ $< -lsystemd

# Запустить проект (автоматически вызовет сборку при необходимости)
run: build
	@echo "--- Запуск приложения ---"
	dotnet run --project $(CS_PROJ_DIR)

# Очистить все созданные файлы
clean:
	@echo "--- Очистка проекта ---"
	rm -f $(C_LIB)
	rm -rf $(CS_PROJ_DIR)/bin
	rm -rf $(CS_PROJ_DIR)/obj
	@echo "Очистка завершена."