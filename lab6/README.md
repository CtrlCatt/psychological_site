# Лабораторная работа №6 — CI в GitHub Actions

> Проект «Рядом»: Angular 20, .NET 10, PostgreSQL 17. Продолжение лабораторной №5 с авторизацией JWT и ролями User / Admin. Deploy / CD не используется.

## Что запускает GitHub Actions

Workflow находится **в корне репозитория** в `.github/workflows/ci.yml` (не внутри папки lab6!). Он автоматически запускается при push, pull request и вручную через Actions → CI → Run workflow.

- **Backend / tests**: .NET 10 → `dotnet restore` → `dotnet build --no-restore` → `dotnet test --no-build --no-restore`.
- **PostgreSQL 17**: временный сервис GitHub Actions. Тесты сами создают временную схему, выполняют `01-schema.sql`, `02-seed.sql`, `03-authentication.sql` и удаляют схему после завершения.
- **Frontend / build**: Node.js 22 → `npm ci` → `npm run build`.
- Никаких внешних серверов, деплоя, постоянных БД и платных ключей.

## Важные исправления относительно исходного ZIP лабораторной №5

1. В `AuthenticationTests.cs` добавлен `using Microsoft.Extensions.Configuration;` для успешной компиляции метода `GetConnectionString`.
2. Добавлена отсутствовавшая в исходном архиве зависимость `@angular/forms@20.3.34` и запись в `package-lock.json` для `npm ci` без конфликта Angular-версий.
3. Для Angular-админки добавлен `ChangeDetectorRef.markForCheck()` в `finally`, чтобы список пользователей обновлялся при завершении запроса.

## Как правильно разместить в GitHub

Из архива перенести в **корень GitHub-репозитория** оба элемента:

```text
.github/workflows/ci.yml
lab6/ ... (проект Angular, backend, database, tests, Ryadom.sln)
```

Если залить папку `.github` внутрь `lab6`, GitHub Actions её НЕ найдёт.

Открой Pull Request из ветки `lab-6` в `main`, затем вкладку **Actions**. Два задания (`Backend / tests` и `Frontend / build`) должны стать зелёными после успешной сборки и тестов. В этом окружении нельзя подтвердить цвет реального запуска — зелёный статус появится только после запуска в GitHub.

## Локальная проверка (Windows, из корня lab6)

```powershell
$env:RYADOM_TEST_DB = 'Host=localhost;Port=5432;Database=ryadom_lab4;Username=postgres;Password=ВАШ_ПАРОЛЬ'
dotnet restore Ryadom.sln
dotnet build Ryadom.sln --no-restore
dotnet test Ryadom.sln --no-build --no-restore
cd frontend
npm.cmd ci
npm.cmd run build
```

`RYADOM_TEST_DB` должен указывать на доступную PostgreSQL-базу с правом `CREATE SCHEMA`.

---

## Подробная инструкция из лабораторной №5

# Лабораторная работа №5 — авторизация и роли

Проект **«Рядом»**: Angular 20 + TypeScript, ASP.NET Core (.NET 10, C#), PostgreSQL 17, Entity Framework Core и параметризованные запросы Npgsql. Лабораторная продолжает №4, не меняя её исходники.

## Что реализовано

- Регистрация `POST /api/auth/register` с проверкой логина и сложности пароля; новой учётной записи назначается роль **User**.
- Вход `POST /api/auth/login`: сервер проверяет хеш пароля и возвращает **JWT Bearer access token** на 60 минут. Логин и пароль передаются **только при входе/регистрации**, а в каждом защищённом HTTP-запросе Angular автоматически отправляет `Authorization: Bearer <token>`.
- **User** может читать каталог, проходить опросы и управлять **только своими** результатами и записями. **Admin** получает дополнительный защищённый список пользователей с пагинацией по 10. При попытке User открыть `GET /api/admin/users` сервер возвращает **403**, без токена — **401**.
- Пароль хранится **только в поле `app_users.password_hash`**. `PasswordHasher<T>` ASP.NET Core создаёт уникальную соль и PBKDF2-хеш. В ответах API, JWT и административном списке открытого пароля или его хеша нет.
- Из API удалён небезопасный `X-Visitor-Id`. Идентификатор владельца берётся исключительно из проверенной сервером подписи JWT (`sub`), поэтому смена заголовка браузером не позволяет открыть чужие данные.
- Каталог (`/api/assessments`, `/api/specialists`) открыт для всех (`public`). Все CRUD-методы `/api/results`, `/api/bookings` защищены `[Authorize]`. Admin endpoint защищён `[Authorize(Roles = "Admin")]`.
- Разграничение доступа, ошибка 401/403, валидация, изоляция данных пользователей, проверка паролей и миграция БД включены в интеграционные тесты.

## 1. PostgreSQL в pgAdmin 4

У тебя уже должна работать база **`ryadom_lab4`** из четвёртой лабораторной. В pgAdmin выбери **Databases → ryadom_lab4 → Query Tool** и один раз выполни:

[`database/03-authentication.sql`](database/03-authentication.sql)

Это добавит таблицу `app_users` и ограничения связей для новых записей. Таблицы и данные лабы №4 не удаляются. Пароли в SQL не прописаны.

Если создаёшь новую базу с нуля: последовательно выполни `00-create-database.sql` в `postgres`, затем `01-schema.sql`, `02-seed.sql` и `03-authentication.sql` в `ryadom_lab4`.

**Важно о старых данных:** в lab4 результаты и записи связывались с анонимным `visitor_id`; в lab5 он заменён идентификатором настоящего пользователя. Старые анонимные данные остаются в таблицах, но автоматически не привязываются к новому аккаунту. Для учебного проекта после регистрации создай новые записи.

## 2. Запуск C# API в VS Code (Windows PowerShell)

Установи [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), PostgreSQL и Node.js. Распакуй архив, открой в VS Code папку **`lab5`**. В первом терминале PowerShell выполни:

```powershell
$env:ConnectionStrings__Ryadom = 'Host=localhost;Port=5432;Database=ryadom_lab4;Username=postgres;Password=ТВОЙ_ПАРОЛЬ_ОТ_POSTGRES'
$env:Jwt__Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/Ryadom.Api --urls http://127.0.0.1:5000
```

В `Jwt__Key` создаётся случайный секрет подписи. **Никогда не сохраняй его в репозитории**, не передавай пароль PostgreSQL преподавателю. Если после перезапуска используется новый ключ, ранее выданные JWT перестанут действовать: достаточно снова войти.

### Как создать администратора для демонстрации

Перед запуском бэкенда дополнительно задай переменные **в том же терминале**:

```powershell
$env:AdminBootstrap__Username = 'teacher_admin'
$env:AdminBootstrap__Password = Read-Host 'Придумай сложный пароль администратора'
```

После этого запускай `dotnet run ...` из предыдущего блока. Администратор создаётся только когда `ASPNETCORE_ENVIRONMENT=Development` и указанного логина ещё нет. Если аккаунт с логином уже существует, система **не повышает его роль**. Пароль сохраняется лишь в хешированном виде. В production bootstrap отключён.

## 3. Запуск Angular

Во **втором** терминале PowerShell из папки `lab5`:

```powershell
cd frontend
npm.cmd ci
npm.cmd start
```

Открой <http://localhost:4200>. В меню выбери **«Войти» → «Нет аккаунта? Зарегистрироваться»**. Создай логин (3–32 латинских символа, цифры и `_`, начиная с буквы) и пароль (10–128 символов, буквы + цифры). После входа личный кабинет, прохождение опросов и запись на консультации работают под твоим пользователем.

Чтобы показать роль **Admin**, войди под `teacher_admin` и паролем, который указал при первом запуске API. В интерфейсе появится пункт **«Админ»** со списком пользователей.

**Примечание по безопасности:** для учебной версии JWT хранится в `sessionStorage` (до закрытия вкладки) и истекает через 60 минут. Это не полноценный production-сценарий: в реальном публичном сервисе обязательны HTTPS, защита от XSS, ограничение частоты попыток входа, управление отзывом сессий и механизм обновления токенов.

## 4. Пример проверки через API

Без токена (сервер должен ответить HTTP 401):

```powershell
curl.exe -i http://127.0.0.1:5000/api/results
```

Публичный каталог (HTTP 200):

```powershell
curl.exe -i 'http://127.0.0.1:5000/api/assessments?page=1&pageSize=10'
```

При входе и регистрации используется JSON с `userName` и `password`. Токен нужно отправлять в `Authorization: Bearer <JWT>`; в Angular это делает `bearerInterceptor` автоматически.

Проверь в pgAdmin, что лежит в базе:

```sql
SELECT username, role, left(password_hash, 12) AS hash_prefix, created_at
FROM app_users ORDER BY created_at DESC LIMIT 10;
```

В результате **нет колонки `password`**, а `hash_prefix` — начало хешированной строки. Хеши не публикуй в скриншотах PR, если используешь реальные аккаунты.

## 5. Тесты

Интеграционные тесты в своей временной PostgreSQL-схеме выполняют SQL-скрипты lab4 и новый скрипт lab5. Укажи доступную PostgreSQL-базу (учётная запись должна иметь право `CREATE SCHEMA`):

```powershell
$env:RYADOM_TEST_DB = 'Host=localhost;Port=5432;Database=ryadom_lab4;Username=postgres;Password=ТВОЙ_ПАРОЛЬ_ОТ_POSTGRES'
dotnet test Ryadom.sln
```

Модульные тесты предметной области без PostgreSQL:

```powershell
dotnet test Ryadom.sln --filter 'FullyQualifiedName~DomainTests'
```

**Тестирование в среде подготовки:** исходники адаптированы, но сборка `dotnet test` не выполнялась — .NET SDK и доступная тестовая PostgreSQL-база здесь отсутствуют. `npm ci` для фронтенда также не завершился из-за недоступности пакетов. Поэтому перед сдачей выполни команды на своём компьютере и приложи скриншоты результатов.

## 6. Структура для Pull Request №5

```text
lab5/
  backend/
    Ryadom.Api/            # JWT, контроллеры, HTTP 401/403
    Ryadom.Application/    # сценарии регистрации, входа и репозитории
    Ryadom.Domain/         # предметная область (из предыдущих лаб)
    Ryadom.Infrastructure/ # EF Core, Npgsql, хранение аккаунтов и хешей
  database/
    00-create-database.sql
    01-schema.sql
    02-seed.sql
    03-authentication.sql
    schema.svg
  frontend/                 # Angular 20, auth guard, interceptor, login, admin
  tests/                    # xUnit + интеграционные проверки PostgreSQL
  README.md
```

Загрузи **только папку `lab5`** в новую ветку `lab-5`. Не загружай `node_modules`, `bin`, `obj` или локальные конфигурации с паролями. Создай отдельный PR **`lab-5 → main`** и проверь содержимое вкладки *Files changed*.
