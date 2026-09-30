-- Sessions and linked provider accounts for the web app's sign-in (Better-Auth). The web app
-- connects as patient_portal_web, which can use only these tables; the API's role cannot see them.
-- Tokens in web_accounts are encrypted by the web app before they are stored.
CREATE TABLE web_users (
    id             text PRIMARY KEY,
    name           text NOT NULL,
    email          text NOT NULL UNIQUE,
    email_verified boolean NOT NULL DEFAULT false,
    image          text,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE web_sessions (
    id         text PRIMARY KEY,
    expires_at timestamptz NOT NULL,
    token      text NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL,
    ip_address text,
    user_agent text,
    user_id    text NOT NULL REFERENCES web_users (id) ON DELETE CASCADE
);
CREATE INDEX web_sessions_user_id_idx ON web_sessions (user_id);

CREATE TABLE web_accounts (
    id                       text PRIMARY KEY,
    account_id               text NOT NULL,
    provider_id              text NOT NULL,
    user_id                  text NOT NULL REFERENCES web_users (id) ON DELETE CASCADE,
    access_token             text,
    refresh_token            text,
    id_token                 text,
    access_token_expires_at  timestamptz,
    refresh_token_expires_at timestamptz,
    scope                    text,
    password                 text,
    created_at               timestamptz NOT NULL DEFAULT now(),
    updated_at               timestamptz NOT NULL
);
CREATE INDEX web_accounts_user_id_idx ON web_accounts (user_id);
CREATE UNIQUE INDEX web_accounts_provider_account_idx ON web_accounts (provider_id, account_id);

CREATE TABLE web_verifications (
    id         text PRIMARY KEY,
    identifier text NOT NULL,
    value      text NOT NULL,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX web_verifications_identifier_idx ON web_verifications (identifier);

-- Like patient_portal_app, created without a password; the migration runner sets it.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'patient_portal_web') THEN
        CREATE ROLE patient_portal_web LOGIN;
    END IF;
END;
$$;

GRANT USAGE ON SCHEMA public TO patient_portal_web;
GRANT SELECT, INSERT, UPDATE, DELETE
    ON web_users, web_sessions, web_accounts, web_verifications
    TO patient_portal_web;
