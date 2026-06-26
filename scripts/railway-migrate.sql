-- =============================================================================
-- GestionSpa — migración manual para consola Postgres de Railway
-- =============================================================================
-- CUÁNDO USARLO:
--   Tu base YA tiene tablas (EnsureCreated) y al redeploy la API falla con
--   "relation already exists" o errores de MigrateAsync.
--
-- CÓMO EJECUTAR:
--   Railway → Postgres → Connect → Query (o psql) → pegar todo → Run
--
-- NO uses esto en una base vacía nueva: ahí alcanza con redeploy de la API
-- (MigrateAsync crea todo solo).
-- =============================================================================

BEGIN;

-- 1) Tabla de historial EF (si no existe)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- 2) Verificar cédulas duplicadas ANTES del índice único
DO $$
DECLARE
    dup_count integer;
BEGIN
    SELECT COUNT(*) INTO dup_count
    FROM (
        SELECT "Cedula"
        FROM "Socios"
        GROUP BY "Cedula"
        HAVING COUNT(*) > 1
    ) d;

    IF dup_count > 0 THEN
        RAISE EXCEPTION
            'Hay % cédula(s) duplicada(s) en Socios. Corregí los datos antes de continuar. '
            'Consulta: SELECT "Cedula", COUNT(*) FROM "Socios" GROUP BY "Cedula" HAVING COUNT(*) > 1;',
            dup_count;
    END IF;
END $$;

-- 3) Índice único de cédula (hardening P1-13)
DROP INDEX IF EXISTS "IX_Socios_Cedula";
CREATE UNIQUE INDEX "IX_Socios_Cedula" ON "Socios" ("Cedula");

-- 4) FK Cargos → Clientes: SetNull → Restrict (hardening P1-14)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_Cargos_Clientes_ClienteId'
          AND table_name = 'Cargos'
    ) THEN
        ALTER TABLE "Cargos" DROP CONSTRAINT "FK_Cargos_Clientes_ClienteId";
    END IF;

    ALTER TABLE "Cargos"
        ADD CONSTRAINT "FK_Cargos_Clientes_ClienteId"
        FOREIGN KEY ("ClienteId") REFERENCES "Clientes" ("Id")
        ON DELETE RESTRICT;
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

-- 5) FK Cargos → Servicios: Restrict (si faltaba)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_Cargos_Servicios_ServicioId'
          AND table_name = 'Cargos'
    ) THEN
        ALTER TABLE "Cargos"
            ADD CONSTRAINT "FK_Cargos_Servicios_ServicioId"
            FOREIGN KEY ("ServicioId") REFERENCES "Servicios" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

-- 6) FK CuotasMensuales → Socios: Restrict
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_CuotasMensuales_Socios_SocioId'
          AND table_name = 'CuotasMensuales'
    ) THEN
        ALTER TABLE "CuotasMensuales" DROP CONSTRAINT "FK_CuotasMensuales_Socios_SocioId";
    END IF;

    ALTER TABLE "CuotasMensuales"
        ADD CONSTRAINT "FK_CuotasMensuales_Socios_SocioId"
        FOREIGN KEY ("SocioId") REFERENCES "Socios" ("Id")
        ON DELETE RESTRICT;
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

-- 7) FK Ingresos → Socios: Restrict
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_Ingresos_Socios_SocioId'
          AND table_name = 'Ingresos'
    ) THEN
        ALTER TABLE "Ingresos"
            ADD CONSTRAINT "FK_Ingresos_Socios_SocioId"
            FOREIGN KEY ("SocioId") REFERENCES "Socios" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

-- 8) FK Pagos (si faltaban)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_Pagos_Cargos_CargoId'
          AND table_name = 'Pagos'
    ) THEN
        ALTER TABLE "Pagos"
            ADD CONSTRAINT "FK_Pagos_Cargos_CargoId"
            FOREIGN KEY ("CargoId") REFERENCES "Cargos" ("Id")
            ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_Pagos_CuotasMensuales_CuotaMensualId'
          AND table_name = 'Pagos'
    ) THEN
        ALTER TABLE "Pagos"
            ADD CONSTRAINT "FK_Pagos_CuotasMensuales_CuotaMensualId"
            FOREIGN KEY ("CuotaMensualId") REFERENCES "CuotasMensuales" ("Id")
            ON DELETE SET NULL;
    END IF;
END $$;

-- 9) Marcar migración EF como aplicada (evita que MigrateAsync recree tablas)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260626211416_InitialCreate', '10.0.9')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- 10) Verificación rápida
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";

SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Socios' AND indexname = 'IX_Socios_Cedula';
