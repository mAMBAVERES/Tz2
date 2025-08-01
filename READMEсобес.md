# GitLab CI/CD Pipeline для .NET приложения - Полное руководство для новичков

## Что делает этот пайплайн

Автоматически собирает .NET приложение под разные операционные системы, тестирует, подписывает и разворачивает на серверах.

## Структура пайплайна

### Стадии выполнения (stages)
```yaml
stages:
  - build    # Сборка под разные архитектуры
  - test     # Запуск тестов
  - sign     # Подпись исполняемых файлов
  - deploy   # Развертывание на серверах
  - upload   # Загрузка в файловое хранилище
```

**Почему именно в таком порядке:**
- Сначала собираем код - без сборки нечего тестировать
- Тестируем собранное приложение - проверяем что оно работает
- Подписываем только после успешных тестов - не подписываем сломанный код
- Деплоим только проверенную версию
- Загружаем артефакты для архива и распространения

## Подробный разбор переменных

### Глобальные переменные (variables)

#### DOCKER_DRIVER: overlay2
```yaml
DOCKER_DRIVER: overlay2
```
**Что это:** Драйвер хранения для Docker.

**Зачем нужен overlay2:**
- **Самая высокая производительность** - быстрее создает и удаляет контейнеры
- **Экономия места** - эффективно переиспользует слои образов
- **Стандарт индустрии** - рекомендуется Docker Inc.
- **Поддержка современных FS** - работает на ext4, xfs, btrfs

**Альтернативы и их проблемы:**
- `aufs` - устаревший, медленный, проблемы с производительностью
- `devicemapper` - сложная настройка, может вызывать зависания
- `vfs` - очень медленный, только для отладки

#### DOCKER_TLS_CERTDIR: "/certs"
```yaml
DOCKER_TLS_CERTDIR: "/certs"
```
**Что это:** Папка для TLS сертификатов Docker.

**Зачем нужно:**
- **Безопасность** - шифрует связь между Docker клиентом и сервером
- **Docker-in-Docker требование** - без этого dind не запустится
- **Защита от перехвата** - предотвращает атаки man-in-the-middle

**Почему "/certs":**
- Стандартная папка в Docker dind образах
- Автоматически создается и настраивается
- Правильные права доступа установлены по умолчанию

#### DOTNET_IMAGE: "mcr.microsoft.com/dotnet/sdk:8.0"
```yaml
DOTNET_IMAGE: "mcr.microsoft.com/dotnet/sdk:8.0"
```
**Что это:** Официальный образ Microsoft с .NET SDK.

**Расшифровка частей:**
- `mcr.microsoft.com` - Microsoft Container Registry (официальный реестр Microsoft)
- `dotnet` - семейство .NET образов
- `sdk` - Software Development Kit (полный набор инструментов разработки)
- `8.0` - версия .NET (LTS - Long Term Support)

**Что включает SDK:**
- .NET Runtime - для запуска приложений
- Компилятор C# - для сборки кода
- MSBuild - система сборки
- NuGet - менеджер пакетов
- dotnet CLI - интерфейс командной строки

**Альтернативы и их назначение:**
- `runtime` - только для запуска (без компилятора)
- `aspnet` - для веб-приложений ASP.NET
- `runtime-deps` - минимальные зависимости

## Подробный разбор шаблона .docker_job

```yaml
.docker_job:
  image: docker:24.0.5
  services:
    - docker:24.0.5-dind
  before_script:
    - docker info
```

### image: docker:24.0.5
**Что это:** Основной контейнер для выполнения команд.

**Почему Docker образ:**
- **Содержит Docker CLI** - позволяет выполнять docker команды
- **Легкий вес** - основан на Alpine Linux (~5MB)
- **Стабильная версия** - 24.0.5 проверена на производстве
- **Изоляция** - каждый job выполняется в чистом окружении

### services: docker:24.0.5-dind
**Что это:** Docker-in-Docker сервис.

**Расшифровка dind:** Docker in Docker

**Как это работает:**
```
GitLab Runner
    ↓
Docker контейнер (docker:24.0.5)
    ↓
Docker демон (dind)
    ↓
.NET контейнер (mcr.microsoft.com/dotnet/sdk:8.0)
```

**Зачем нужен dind:**
- **Изоляция** - каждая сборка в отдельном окружении
- **Чистота** - нет влияния предыдущих сборок
- **Безопасность** - не доступ к основной системе
- **Параллельность** - несколько сборок одновременно

### before_script: docker info
**Что делает:** Проверяет работоспособность Docker.

**Что показывает docker info:**
- Версия Docker Engine
- Драйвер хранения (должен быть overlay2)
- Количество контейнеров и образов
- Доступную память и процессор

**Зачем нужна проверка:**
- **Диагностика** - сразу видно если Docker не работает
- **Отладка** - помогает найти причину проблем
- **Мониторинг** - показывает состояние системы

## Подробный разбор сборки (build jobs)

### Команда сборки Linux
```yaml
script:
  - |
    docker run --rm -v $(pwd):/workspace -w /workspace $DOTNET_IMAGE bash -c "
      dotnet restore
      dotnet publish -c Release -r linux-x64 --self-contained -o /workspace/publish/linux-x64
    "
```

#### Разбор docker run команды:

**docker run** - запуск нового контейнера

**--rm** - автоматически удалить контейнер после завершения
- **Зачем:** Экономит место на диске
- **Альтернатива:** Без --rm контейнеры накапливаются и засоряют систему

**-v $(pwd):/workspace** - монтирование директории
- `$(pwd)` - текущая папка на GitLab Runner (где лежит исходный код)
- `:/workspace` - папка внутри контейнера
- **Результат:** Контейнер видит наш код как папку /workspace

**-w /workspace** - установка рабочей директории
- **Зачем:** Все команды будут выполняться из этой папки
- **Альтернатива:** Без -w пришлось бы писать `cd /workspace` в каждой команде

**$DOTNET_IMAGE** - использование переменной с образом
- **Зачем переменная:** Легко изменить версию .NET в одном месте
- **Подстановка:** GitLab заменит на `mcr.microsoft.com/dotnet/sdk:8.0`

**bash -c "команды"** - выполнение команд в bash
- **Зачем bash:** Позволяет выполнить несколько команд последовательно
- **-c** - выполнить команды из строки, а не из файла

#### Разбор .NET команд:

**dotnet restore**
```bash
dotnet restore
```
**Что делает:** Скачивает зависимости проекта (NuGet пакеты).

**Зачем нужно:**
- Без зависимостей код не скомпилируется
- Скачивает библиотеки указанные в .csproj файле
- Создает lock файлы для воспроизводимых сборок

**Что происходит:**
1. Читает .csproj файл
2. Анализирует зависимости
3. Скачивает пакеты из NuGet.org
4. Сохраняет в локальный кэш

**Аналоги в других языках:**
- `npm install` в Node.js
- `pip install -r requirements.txt` в Python
- `go mod download` в Go

**dotnet publish**
```bash
dotnet publish -c Release -r linux-x64 --self-contained -o /workspace/publish/linux-x64
```

**Разбор параметров:**

**-c Release** - конфигурация сборки
- **Release** - оптимизированная версия для продакшена
- **Альтернатива:** Debug - с отладочной информацией, больше размер
- **Оптимизации:** Удаление неиспользуемого кода, сжатие, оптимизация производительности

**-r linux-x64** - целевая платформа (Runtime Identifier)
- **linux** - операционная система
- **x64** - архитектура процессора (64-bit)
- **Другие примеры:** win-x64, win-x86, osx-x64, linux-arm64

**--self-contained** - включить .NET runtime
- **Что включается:** Все библиотеки .NET нужные для запуска
- **Плюсы:** Не нужно устанавливать .NET на целевой машине
- **Минусы:** Больший размер файлов (~70MB вместо ~5MB)
- **Альтернатива:** `--no-self-contained` (требует .NET на сервере)

**-o /workspace/publish/linux-x64** - выходная папка
- **Куда сохраняется:** Готовое к запуску приложение
- **Что содержит:** Исполняемые файлы, библиотеки, конфигурации

### Артефакты (artifacts)
```yaml
artifacts:
  paths:
    - publish/linux-x64/
  expire_in: 1 day
```

**paths: - publish/linux-x64/**
- **Что сохраняется:** Папка с собранным приложением
- **Куда:** В GitLab внутреннее хранилище
- **Доступ:** Можно скачать через веб-интерфейс
- **Передача:** Автоматически передается следующим jobs

**expire_in: 1 day**
- **Сколько хранить:** 1 день
- **Зачем ограничение:** Экономия места на GitLab сервере
- **Варианты:** 1 hour, 1 week, 1 month, never
- **Рекомендация:** Для разработки 1-7 дней, для релизов 1-6 месяцев

## Подробный разбор тестирования

### Зависимости тестов
```yaml
test_dotnet:
  needs:
    - job: "build_linux_x64"
      artifacts: true
```

**needs: - job: "build_linux_x64"**
- **Что означает:** Тесты не запустятся пока не завершится build_linux_x64
- **Зачем:** Тестировать нечего без собранного приложения
- **Поведение:** Если сборка упадет - тесты не запустятся

**artifacts: true**
- **Что делает:** Скачивает папку publish/linux-x64/ от build_linux_x64
- **Зачем:** Тестам нужны собранные файлы
- **Куда скачивается:** В рабочую папку текущего job

### Команда тестирования
```yaml
script:
  - |
    docker run --rm -v $(pwd):/workspace -w /workspace $DOTNET_IMAGE bash -c "
      dotnet test --configuration Release --logger trx --results-directory TestResults
    "
```

**dotnet test** - запуск тестов

**--configuration Release**
- **Зачем:** Тестируем ту же конфигурацию что собирали
- **Консистентность:** Поведение Release может отличаться от Debug

**--logger trx**
- **Что это:** Test Results XML - формат Microsoft для результатов тестов
- **Зачем:** GitLab понимает этот формат и показывает красивые отчеты
- **Альтернативы:** junit, console, html

**--results-directory TestResults**
- **Куда сохранять:** В папку TestResults
- **Формат файлов:** *.trx файлы с результатами каждого тест-проекта

### Артефакты тестов
```yaml
artifacts:
  when: always
  paths:
    - TestResults/
  reports:
    junit: TestResults/*.trx
  expire_in: 1 week
```

**when: always**
- **Что означает:** Сохранять артефакты даже если тесты провалились
- **Зачем:** Нужно видеть результаты неудачных тестов для отладки
- **Альтернативы:** on_success (только при успехе), on_failure (только при ошибке)

**reports: junit: TestResults/*.trx**
- **Что делает:** Парсит .trx файлы и показывает в GitLab UI
- **Где видно:** Merge Request → Tests tab, Pipeline → Tests
- **Возможности:** Видно какие тесты прошли/упали, время выполнения

## Подробный разбор подписи (sign)

### Зависимости подписи
```yaml
sign_applications:
  needs:
    - job: "build_windows_x64"
      artifacts: true
    - job: "build_windows_x86"
      artifacts: true
    - job: "test_dotnet"
```

**Множественные зависимости:**
- **build_windows_x64** - нужны Windows x64 файлы для подписи
- **build_windows_x86** - нужны Windows x86 файлы для подписи  
- **test_dotnet** - подписываем только протестированный код

**Логика выполнения:**
1. Ждем завершения всех трех jobs
2. Если хотя бы один упал - подпись не запускается
3. Скачиваем артефакты от Windows сборок

### Команды подписи
```yaml
script:
  - echo "Подпись .NET приложений"
  - find publish/win-x64 -name "*.exe" -exec echo "Подписываем {}" \;
  - find publish/win-x86 -name "*.exe" -exec echo "Подписываем {}" \;
```

**find publish/win-x64 -name "*.exe"**
- **find** - поиск файлов в папке
- **publish/win-x64** - где искать
- **-name "*.exe"** - критерий поиска (файлы с расширением .exe)

**-exec echo "Подписываем {}" \;**
- **-exec** - выполнить команду для каждого найденного файла
- **{}** - заменяется на имя найденного файла
- **\;** - завершение команды (экранированная точка с запятой)

**В реальности вместо echo должно быть:**
```bash
# Windows SignTool
signtool sign /f certificate.p12 /p password /t http://timestamp.server.com {}

# Или Linux osslsigncode
osslsigncode sign -certs cert.pem -key key.pem -in {} -out {}.signed
```

### Правила подписи
```yaml
rules:
  - if: '$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_REF_NAME == "main"'
  - if: '$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_TAG'
```

**Условие 1:** `$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_REF_NAME == "main"`
- **$REPOSITORY_NAME** - переменная с именем репозитория (устанавливается вручную)
- **$CI_COMMIT_REF_NAME** - встроенная переменная GitLab с именем ветки
- **Логика:** Подпись только для основной ветки основного репозитория

**Условие 2:** `$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_TAG`
- **$CI_COMMIT_TAG** - встроенная переменная, содержит тег если коммит помечен
- **Логика:** Подпись для всех тегов (релизов) основного репозитория

**Зачем два условия:**
- **main ветка** - ночные сборки, pre-release версии
- **теги** - официальные релизы (v1.0.0, v2.1.3)

## Подробный разбор деплоя

### Staging деплой
```yaml
deploy_staging:
  needs:
    - job: "build_linux_x64"
      artifacts: true
    - job: "test_dotnet"
  environment:
    name: staging
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_REF_NAME == "develop"'
```

**Зависимости staging:**
- **build_linux_x64** - нужны Linux файлы (серверы обычно на Linux)
- **test_dotnet** - деплоим только протестированный код
- **НЕ НУЖНА подпись** - staging для внутреннего тестирования

**environment: name: staging**
- **Что это:** Логическое окружение в GitLab
- **Возможности:** 
  - История деплоев
  - Мониторинг статуса
  - Быстрый rollback
  - Интеграция с внешними системами мониторинга

**Правило:** `$CI_COMMIT_REF_NAME == "develop"`
- **develop ветка** - ветка для интеграции новых функций
- **Автоматический деплой** - каждый коммит в develop идет на staging
- **Цель:** Быстрая проверка новых функций командой QA

### Production деплой
```yaml
deploy_production:
  needs:
    - job: "sign_applications"
      artifacts: true
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app" && $CI_COMMIT_REF_NAME == "main"'
      when: manual
```

**Зависимость:** `sign_applications`
- **Только подписанный код** - безопасность и соответствие политикам
- **Включает все платформы** - sign_applications содержит все собранные файлы

**when: manual**
- **Ручной запуск** - предотвращает случайный деплой в продакшен
- **В GitLab UI** - появляется кнопка "Deploy" которую нужно нажать
- **Контроль доступа** - только авторизованные пользователи могут деплоить

## Подробный разбор загрузки (upload)

### Зависимости загрузки
```yaml
upload_artifacts:
  needs:
    - job: "build_linux_x64"
      artifacts: true
    - job: "build_windows_x64"
      artifacts: true
    - job: "build_windows_x86"
      artifacts: true
```

**Три зависимости:**
- Получаем артефакты от всех сборок
- Упаковываем все платформы в архивы
- Загружаем для распространения

### Команды упаковки
```yaml
script:
  - mkdir -p upload
  - tar -czf upload/linux-x64.tar.gz -C publish linux-x64/
  - cd publish && zip -r ../upload/win-x64.zip win-x64/ && cd ..
  - cd publish && zip -r ../upload/win-x86.zip win-x86/ && cd ..
```

**mkdir -p upload**
- **mkdir** - создать папку
- **-p** - создать родительские папки если не существуют, не ошибаться если папка уже есть

**tar -czf upload/linux-x64.tar.gz -C publish linux-x64/**
- **tar** - архиватор Unix/Linux
- **-c** - create (создать архив)
- **-z** - gzip (сжать)
- **-f upload/linux-x64.tar.gz** - имя файла архива
- **-C publish** - перейти в папку publish перед архивацией
- **linux-x64/** - что архивировать

**Зачем -C publish:**
- Без -C: архив содержит `publish/linux-x64/файлы`
- С -C: архив содержит `linux-x64/файлы`
- Результат: чище структура при распаковке

**zip -r ../upload/win-x64.zip win-x64/**
- **zip** - архиватор Windows/универсальный
- **-r** - recursive (рекурсивно, включая подпапки)
- **../upload/win-x64.zip** - путь к создаваемому архиву
- **win-x64/** - что архивировать

**Зачем разные форматы:**
- **tar.gz для Linux** - стандарт в Unix системах, лучше сжатие
- **zip для Windows** - стандарт в Windows, встроенная поддержка

### MinIO загрузка
```yaml
- |
  if [ -n "$MINIO_ENDPOINT" ]; then
    docker run --rm -v $(pwd):/workspace \
      -e MC_HOST_minio="$MINIO_ENDPOINT" \
      minio/mc:latest \
      mc cp --recursive /workspace/upload/ minio/$MINIO_BUCKET/$CI_COMMIT_REF_NAME/
  else
    echo "Файлы готовы в upload/"
    ls -la upload/
  fi
```

**if [ -n "$MINIO_ENDPOINT" ]**
- **-n** - проверка что переменная не пустая
- **Логика:** Если MinIO настроен - загружаем, если нет - просто показываем файлы
- **Graceful degradation:** Пайплайн не падает если MinIO недоступен

**docker run minio/mc:latest**
- **minio/mc** - официальный клиент MinIO (аналог AWS CLI для S3)
- **mc** - MinIO Client

**-e MC_HOST_minio="$MINIO_ENDPOINT"**
- **-e** - передача переменной окружения в контейнер
- **MC_HOST_minio** - стандартная переменная MinIO клиента для настройки подключения
- **$MINIO_ENDPOINT** - адрес MinIO сервера (например: https://minio.company.com)

**mc cp --recursive /workspace/upload/ minio/$MINIO_BUCKET/$CI_COMMIT_REF_NAME/**
- **mc cp** - копирование файлов (аналог aws s3 cp)
- **--recursive** - копировать папки целиком
- **minio/** - псевдоним для MinIO сервера
- **$MINIO_BUCKET** - имя bucket (например: "builds")
- **$CI_COMMIT_REF_NAME** - имя ветки (создает папку по имени ветки)

**Результат в MinIO:**
```
builds/
├── main/
│   ├── linux-x64.tar.gz
│   ├── win-x64.zip
│   └── win-x86.zip
├── develop/
│   ├── linux-x64.tar.gz
│   ├── win-x64.zip
│   └── win-x86.zip
└── feature-auth/
    ├── linux-x64.tar.gz
    ├── win-x64.zip
    └── win-x86.zip
```

## Переменные окружения для настройки

### Обязательные переменные
В GitLab проект → Settings → CI/CD → Variables добавить:

**REPOSITORY_NAME**
- **Значение:** `dotnet-app`
- **Тип:** Variable
- **Защищенная:** Нет
- **Зачем:** Безопасность - пайплайн запускается только для определенного репозитория

### Опциональные переменные для MinIO

**MINIO_ENDPOINT**
- **Значение:** `https://minio.company.com`
- **Тип:** Variable
- **Защищенная:** Нет
- **Описание:** Адрес MinIO сервера

**MINIO_BUCKET**
- **Значение:** `builds`
- **Тип:** Variable
- **Защищенная:** Нет
- **Описание:** Имя bucket для хранения артефактов

**MINIO_ACCESS_KEY** (если используется)
- **Значение:** `admin`
- **Тип:** Variable
- **Защищенная:** Да
- **Описание:** Ключ доступа к MinIO

**MINIO_SECRET_KEY** (если используется)
- **Значение:** `secretpassword`
- **Тип:** Variable
- **Защищенная:** Да
- **Маскированная:** Да
- **Описание:** Секретный ключ MinIO

## Логика выполнения пайплайна

### Сценарий 1: Ветка develop
```
build_linux_x64  ──┐
build_windows_x64 ──┼─→ test_dotnet ──→ deploy_staging
build_windows_x86  ──┘       ↓
                              ↓
                         upload_artifacts
```

**Что происходит:**
1. Параллельно собираются 3 версии приложения
2. Тесты запускаются после Linux сборки
3. После успешных тестов - автоматический деплой на staging
4. Загрузка всех артефактов в MinIO

### Сценарий 2: Ветка main
```
build_linux_x64  ──┐
build_windows_x64 ──┼─→ test_dotnet ──→ sign_applications ──→ deploy_production (manual)
build_windows_x86  ──┘       ↓                    ↓
                              ↓                    ↓
                         upload_artifacts    (подписанные файлы)
```

**Что происходит:**
1. Параллельно собираются 3 версии приложения
2. Тесты запускаются после Linux сборки
3. После успешных тестов - подпись Windows файлов
4. Ручной деплой в production (только подписанные файлы)
5. Загрузка всех артефактов в MinIO

### Сценарий 3: Feature ветка
```
build_linux_x64  ──┐
build_windows_x64 ──┼─→ test_dotnet
build_windows_x86  ──┘       ↓
                              ↓
                         upload_artifacts
```

**Что происходит:**
1. Сборка и тестирование
2. Загрузка артефактов для тестирования
3. Никакого деплоя

## Возможные проблемы и решения

### 1. "This job is stuck because you don't have any active runners"
**Проблема:** Нет настроенных GitLab Runner'ов.
**Решение:** 
```bash
# Установить GitLab Runner
sudo gitlab-runner register
# Выбрать executor: docker
# Image: docker:24.0.5
```

### 2. Структура веток
```
main (production)     ──→ v1.0.0 ──→ v1.1.0
  ↑
develop (staging)     ──→ новые фичи ──→ тестирование
  ↑
feature/auth         ──→ разработка ──→ merge в develop
feature/payments     ──→ разработка ──→ merge в develop
```

### 3. Время хранения артефактов
- **Feature ветки:** 1 день - экономия места
- **Develop:** 1 неделя - для тестирования
- **Main/теги:** 1 месяц - для релизов
- **Production артефакты:** 6 месяцев - для rollback

### 4. Безопасность
- **Никогда не коммитить** секреты в код
- **Использовать GitLab Variables** для паролей и ключей
- **Отмечать переменные как Protected** для production
- **Регулярно ротировать** ключи доступа

### 5. Мониторинг
- **Настроить алерты** на падение пайплайнов
- **Отслеживать время выполнения** - если растет, оптимизировать
- **Проверять использование ресурсов** Runner'ов
- **Мониторить размер артефактов**

## Быстрый старт

### Шаг 1: Подготовка проекта
```bash
# Создать .NET проект
dotnet new console -n MyApp
cd MyApp

# Создать тестовый проект
dotnet new xunit -n MyApp.Tests
dotnet add MyApp.Tests reference MyApp

# Создать solution
dotnet new sln
dotnet sln add MyApp MyApp.Tests
```

### Шаг 2: Настройка GitLab
1. Создать репозиторий в GitLab
2. Загрузить `.gitlab-ci.yml` из этого руководства
3. В Settings → CI/CD → Variables добавить:
   - `REPOSITORY_NAME` = `dotnet-app`
4. Опционально для MinIO:
   - `MINIO_ENDPOINT` = `https://your-minio.com`
   - `MINIO_BUCKET` = `builds`

### Шаг 3: Настройка Runner (если нужно)
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install docker.io
sudo systemctl start docker

# Установка GitLab Runner
sudo curl -L --output /usr/local/bin/gitlab-runner https://gitlab-runner-downloads.s3.amazonaws.com/latest/binaries/gitlab-runner-linux-amd64
sudo chmod +x /usr/local/bin/gitlab-runner

# Регистрация
sudo gitlab-runner register
# URL: https://gitlab.com/
# Token: из GitLab Settings → CI/CD → Runners
# Executor: docker
# Image: docker:24.0.5

# Настройка для Docker-in-Docker
sudo nano /etc/gitlab-runner/config.toml
# Добавить: privileged = true
```

### Шаг 4: Первый коммит
```bash
git add .
git commit -m "Add CI/CD pipeline"
git push origin develop
```

### Шаг 5: Проверка работы
1. GitLab → CI/CD → Pipelines
2. Найти запущенный пайплайн
3. Кликнуть для просмотра деталей
4. Проверить что все jobs завершились успешно

## Расширение функциональности

### 1. Добавление новых архитектур
```yaml
# ARM64 для современных серверов
build_linux_arm64:
  extends: .docker_job
  stage: build
  script:
    - |
      docker run --rm -v $(pwd):/workspace -w /workspace $DOTNET_IMAGE bash -c "
        dotnet restore
        dotnet publish -c Release -r linux-arm64 --self-contained -o /workspace/publish/linux-arm64
      "
  artifacts:
    paths:
      - publish/linux-arm64/
    expire_in: 1 day
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app"'
```

### 2. Интеграция с Docker Registry
```yaml
build_docker:
  stage: build
  script:
    - docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHORT_SHA .
    - docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHORT_SHA
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app"'
```

### 3. Добавление статического анализа
```yaml
code_quality:
  stage: test
  image: $DOTNET_IMAGE
  script:
    - dotnet tool install --global dotnet-sonarscanner
    - dotnet sonarscanner begin /k:"$CI_PROJECT_NAME"
    - dotnet build
    - dotnet sonarscanner end
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app"'
```

### 4. Добавление безопасности
```yaml
security_scan:
  stage: test
  image: $DOTNET_IMAGE
  script:
    - dotnet list package --vulnerable --include-transitive
    - dotnet list package --deprecated
  allow_failure: true
  rules:
    - if: '$REPOSITORY_NAME == "dotnet-app"'
```

## Оптимизация производительности

### 1. Кэширование зависимостей
```yaml
.docker_job:
  cache:
    key: "$CI_COMMIT_REF_SLUG"
    paths:
      - ~/.nuget/packages/
```

### 2. Параллельное выполнение
- Сборки для разных архитектур выполняются параллельно
- Тесты запускаются сразу после первой сборки
- Подпись и загрузка не блокируют друг друга

### 3. Использование образов с предустановленными зависимостями
```yaml
variables:
  DOTNET_IMAGE: "your-registry/dotnet-with-tools:8.0"
```

### 4. Ограничение артефактов
- Удалять временные файлы перед сохранением
- Сжимать большие файлы
- Использовать короткие сроки хранения для development

## Мониторинг и метрики

### 1. Ключевые метрики
- **Время сборки** - должно быть < 10 минут
- **Процент успешных пайплайнов** - должен быть > 95%
- **Время от коммита до деплоя** - цель < 30 минут
- **Размер артефактов** - отслеживать рост

### 2. Алерты
- Падение пайплайна в main ветке
- Превышение времени выполнения
- Заполнение дискового пространства Runner'ов
- Недоступность MinIO или других внешних сервисов

### 3. Отчеты
- Еженедельный отчет по качеству кода
- Статистика покрытия тестами
- Анализ производительности сборок
- Обзор уязвимостей в зависимостях

## Заключение

Этот пайплайн предоставляет полный цикл разработки:

**✅ Автоматическая сборка** под 3 архитектуры  
**✅ Тестирование** с отчетами в GitLab  
**✅ Подпись** исполняемых файлов  
**✅ Деплой** в staging и production  
**✅ Архивирование** в MinIO  
**✅ Безопасность** через проверку репозитория  

**Преимущества:**
- **Надежность** - каждый шаг проверяется
- **Безопасность** - подпись и ограничения доступа
- **Скорость** - параллельное выполнение
- **Простота** - минималистичная конфигурация
- **Гибкость** - легко расширять и модифицировать

**Подходит для:**
- .NET Core/Framework приложения
- Desktop приложения (WPF, WinForms)
- Console утилиты
- Микросервисы
- API сервисы

Следуйте этому руководству и вы получите профессиональную систему CI/CD для ваших .NET проектов! "docker: command not found"
**Проблема:** Docker не установлен на Runner.
**Решение:**
```bash
sudo apt install docker.io
sudo systemctl start docker
sudo usermod -aG docker gitlab-runner
```

### 3. "Cannot connect to the Docker daemon"
**Проблема:** Runner не имеет прав на Docker или dind не запустился.
**Решение:** В `/etc/gitlab-runner/config.toml` добавить:
```toml
[[runners]]
  [runners.docker]
    privileged = true
    volumes = ["/cache", "/certs/client"]
```

### 4. "dotnet: command not found"
**Проблема:** .NET SDK не найден в контейнере.
**Решение:** Проверить правильность образа в `DOTNET_IMAGE`.

### 5. "No test is available"
**Проблема:** В проекте нет тестов.
**Решение:** Создать тестовый проект:
```bash
dotnet new xunit -n MyApp.Tests
dotnet add MyApp.Tests reference MyApp
```

### 6. Тесты не находят артефакты
**Проблема:** `artifacts: true` не указан в needs.
**Решение:** Проверить что у test_dotnet есть:
```yaml
needs:
  - job: "build_linux_x64"
    artifacts: true  # Важно!
```

### 7. MinIO недоступен
**Проблема:** Ошибка подключения к MinIO.
**Решение:** Пайплайн продолжит работу без загрузки. Проверить:
- MINIO_ENDPOINT корректный
- Сервер доступен из GitLab Runner
- Правильные credentials

## Рекомендации по использованию

### 1. Версионирование
Используйте семантическое версионирование:
- **v1.0.0** - major релиз
- **v1.1.0** - новые функции
- **v1.1.1** - исправления ошибок

### 2.