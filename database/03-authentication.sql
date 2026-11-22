-- Выполнить В БАЗЕ ryadom_lab4 один раз ПОСЛЕ скриптов лабораторной №4.
-- Пароли здесь намеренно отсутствуют: ASP.NET Core сохраняет только salted hash.
BEGIN;
CREATE TABLE IF NOT EXISTS app_users (
    id uuid PRIMARY KEY,
    username varchar(32) NOT NULL CHECK (username ~ '^[A-Za-z][A-Za-z0-9_]{2,31}$'),
    username_normalized varchar(32) NOT NULL,
    password_hash text NOT NULL CHECK (length(password_hash) >= 40),
    role varchar(16) NOT NULL CHECK (role IN ('User', 'Admin')),
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_app_users_normalized UNIQUE (username_normalized)
);
-- Существующая visitor_id в lab4 становится идентификатором пользователя из JWT.
-- Исторические анонимные записи lab4 не переходят в новый аккаунт автоматически.
-- Для имеющихся строк lab4 FK добавляются с NOT VALID:
-- исторические анонимные записи остаются, а новые обязаны ссылаться на app_users.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_results_user' AND conrelid = 'assessment_results'::regclass) THEN
        ALTER TABLE assessment_results ADD CONSTRAINT fk_results_user
            FOREIGN KEY (visitor_id) REFERENCES app_users(id) NOT VALID;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_bookings_user' AND conrelid = 'bookings'::regclass) THEN
        ALTER TABLE bookings ADD CONSTRAINT fk_bookings_user
            FOREIGN KEY (visitor_id) REFERENCES app_users(id) NOT VALID;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_app_users_created ON app_users(created_at DESC);
COMMIT;
