-- Baseline para bases creadas con EnsureCreated antes de migraciones EF.
-- Ejecutar UNA VEZ si MigrateAsync falla porque las tablas ya existen.
-- Reemplazar el nombre de la migración si cambia el timestamp del archivo en Migrations/.

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260626211416_InitialCreate', '10.0.9'
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260626211416_InitialCreate'
);

-- Índice único de cédula (si la tabla ya existía sin él):
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Socios_Cedula" ON "Socios" ("Cedula");
