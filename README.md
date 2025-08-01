### ПАЙПАЙН ДЛЯ ТЗ ###

## Что делает

- Собирает приложение Linux x64, Winx64, Winx86
- Запускает тесты
- Подписывает исполняемые файлы (только для main ветки)
- Деплоит в staging (develop ветка) и production (main ветка)
- Загружает артефакты в MinIO


### Логика работы

### Ветка develop
```
build_linux_x64   ──┐
build_windows_x64 ──┼─→ test_dotnet ──→ deploy_staging
build_windows_x86 ──┘       ↓
                            ↓
                         upload_artifacts
```

**Что происходит:**
1. Параллельно собираются 3 версии приложения
2. Тесты запускаются только после завершения всех сборок
3. После успешных тестов - автоматический деплой на staging
4. Загрузка всех артефактов в MinIO

### Ветка main  
```
build_linux_x64   ──┐
build_windows_x64 ──┼─→ test_dotnet ──→ sign_applications ──→ deploy_production (manual)
build_windows_x86 ──┘       ↓                    ↓
                            ↓                    ↓
                         upload_artifacts    (подписанные файлы)
```

**Что происходит:**
1. Параллельно собираются 3 версии приложения
2. Тесты запускаются только после завершения всех сборок
3. После успешных тестов - подпись Windows файлов
4. Ручной деплой в production (только подписанные файлы)
5. Загрузка всех артефактов в MinIO

### Feature ветки
```
build_linux_x64   ──┐
build_windows_x64 ──┼─→ test_dotnet
build_windows_x86 ──┘       ↓
                            ↓
                        upload_artifacts
```

**Что происходит:**
1. Параллельно собираются 3 версии приложения
2. Тесты запускаются только после завершения всех сборок
3. Загрузка всех артефактов в MinIO
4. Никакого деплоя

## Требования

- .NET проект с файлом `.csproj`
- GitLab Runner с Docker
- Переменная `REPOSITORY_NAME` в настройках GitLab

## Опциональные настройки

Для загрузки в MinIO добавить переменные:
```
MINIO_ENDPOINT = https://your-minio-server.com
MINIO_BUCKET = builds
```

## Результат

После успешного выполнения получите:
- Собранные приложения для 3 платформ
- Отчеты о тестах
- Подписанные файлы (для main)
- Архивы в MinIO
- Развернутое приложение на серверах

## Структура файлов

```
publish/
├── linux-x64/     # Linux приложение
├── win-x64/       # Windows 64-bit
└── win-x86/       # Windows 32-bit

upload/
├── linux-x64.tar.gz
├── win-x64.zip
└── win-x86.zip
```
