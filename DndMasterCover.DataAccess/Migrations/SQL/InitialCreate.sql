﻿DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'dnd') THEN
        CREATE SCHEMA dnd;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS dnd."__MigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'dnd') THEN
        CREATE SCHEMA dnd;
    END IF;
END $EF$;

CREATE TABLE dnd.enemies (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    external_id character varying(150) NOT NULL,
    name character varying(150) NOT NULL,
    danger_level real NOT NULL,
    hp integer NOT NULL,
    max_hp integer,
    class character varying(150) NOT NULL,
    description character varying(3000) NOT NULL,
    abilities jsonb,
    CONSTRAINT pk_enemies PRIMARY KEY (id)
);

CREATE TABLE dnd.enemy_searches (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    name text NOT NULL,
    danger real NOT NULL,
    link text NOT NULL,
    CONSTRAINT pk_enemy_searches PRIMARY KEY (id)
);

CREATE INDEX ix_enemy_searches_name ON dnd.enemy_searches USING GIN (to_tsvector('russian', name));

INSERT INTO dnd."__MigrationsHistory" (migration_id, product_version)
VALUES ('20250219213124_InitialCreate', '9.0.2');

COMMIT;

