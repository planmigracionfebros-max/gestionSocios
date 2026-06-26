-- Reset completo de datos para ciclo QA (conserva __EFMigrationsHistory).
-- Ejecutar en consola Postgres Railway antes de retestear.

BEGIN;

TRUNCATE TABLE
  "Pagos",
  "Ingresos",
  "Cargos",
  "CuotasMensuales",
  "Socios",
  "Clientes",
  "Servicios"
RESTART IDENTITY CASCADE;

COMMIT;

SELECT 'Pagos' AS tabla, COUNT(*) AS registros FROM "Pagos"
UNION ALL SELECT 'Ingresos', COUNT(*) FROM "Ingresos"
UNION ALL SELECT 'Cargos', COUNT(*) FROM "Cargos"
UNION ALL SELECT 'CuotasMensuales', COUNT(*) FROM "CuotasMensuales"
UNION ALL SELECT 'Socios', COUNT(*) FROM "Socios"
UNION ALL SELECT 'Clientes', COUNT(*) FROM "Clientes"
UNION ALL SELECT 'Servicios', COUNT(*) FROM "Servicios";
