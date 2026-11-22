# Маршруты и права доступа

| HTTP-метод | Путь | Доступ | Ответ |
|---|---|---|---|
| POST | `/api/auth/register` | Гость | `201`, JWT |
| POST | `/api/auth/login` | Гость | `200`, JWT / `401` неверные данные |
| GET | `/api/assessments`, `/api/assessments/{id}` | Все | `200` / `404` |
| GET | `/api/specialists`, `/api/specialists/{id}`, `/api/specialists/{id}/slots` | Все | `200` / `404` |
| GET, POST, DELETE | `/api/results`, `/api/results/{id}` | User, Admin | `200`, `201`, `204` / `401`, `404` |
| GET, POST, PUT, DELETE | `/api/bookings`, `/api/bookings/{id}` | User, Admin | `200`, `201`, `204` / `401`, `404`, `409` |
| GET | `/api/admin/users` | Admin | `200` / `401`, `403` |

Ни один защищённый endpoint не принимает userId или visitorId от клиента. Идентификатор пользователя извлекается из проверенного `sub` JWT.

### Архитектура

- `Domain`: предметные правила оценок и бронирования, без HTTP/SQL.
- `Application`: регистрация, вход, проверка пароля через интерфейс, выборка аккаунтов и сценарии предыдущих лабораторных.
- `Infrastructure`: `EfAccountRepository`, `app_users`, EF Core, `EfCatalogRepository`, `EfResultRepository`, `SqlBookingRepository`.
- `Api`: JWT issuing/validation, `[Authorize]`, `AuthController`, `AdminController`, обработка HTTP-ошибок.
- `Angular`: `AuthService`, `bearerInterceptor`, `userGuard`, `adminGuard`, страницы Login и Admin, маршруты `/account`, `/admin`.

### Проверка преподавателем

1. `GET /api/results` без токена → `401`.
2. Регистрация User → `201`, JWT, пароль только в виде salted hash в `app_users`.
3. Вход с неверным паролем → `401`.
4. С токеном User `GET /api/admin/users` → `403`.
5. Вход Admin, `GET /api/admin/users?pageSize=10` → `200` и без password hash.
6. Поддельный `X-Visitor-Id` не меняет владельца результатов; JWT `sub` остаётся единственным источником ID.
7. Перезапуск API не удаляет пользователей/историю, так как они в PostgreSQL.
