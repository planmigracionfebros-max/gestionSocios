-- Limpieza de datos QA antes del go-live en producción.
-- Ejecutar manualmente en PostgreSQL (Railway psql o cliente SQL).
-- REVISAR los IDs antes de ejecutar en producción con datos reales.

BEGIN;

-- Eliminar pagos asociados a cargos/cuotas de prueba
DELETE FROM "Pagos"
WHERE "CargoId" IN (SELECT "Id" FROM "Cargos" WHERE "Monto" < 0 OR "Notas" ILIKE '%test%' OR "Notas" ILIKE '%qa%');

DELETE FROM "Pagos"
WHERE "CuotaMensualId" IN (
  SELECT c."Id" FROM "CuotasMensuales" c
  JOIN "Socios" s ON s."Id" = c."SocioId"
  WHERE s."NumeroSocio" IN ('1006', '1007') OR CAST(s."NumeroSocio" AS INTEGER) >= 1006
);

-- Eliminar cargos de prueba (montos negativos o socios QA)
DELETE FROM "Cargos"
WHERE "Monto" < 0
   OR "Notas" ILIKE '%test%'
   OR "Notas" ILIKE '%qa%'
   OR "SocioId" IN (
     SELECT "Id" FROM "Socios"
     WHERE "NumeroSocio" IN ('1006', '1007') OR CAST("NumeroSocio" AS INTEGER) >= 1006
   );

-- Eliminar cuotas de socios QA
DELETE FROM "CuotasMensuales"
WHERE "SocioId" IN (
  SELECT "Id" FROM "Socios"
  WHERE "NumeroSocio" IN ('1006', '1007') OR CAST("NumeroSocio" AS INTEGER) >= 1006
);

-- Eliminar ingresos de socios QA
DELETE FROM "Ingresos"
WHERE "SocioId" IN (
  SELECT "Id" FROM "Socios"
  WHERE "NumeroSocio" IN ('1006', '1007') OR CAST("NumeroSocio" AS INTEGER) >= 1006
);

-- Eliminar socios de prueba
DELETE FROM "Socios"
WHERE "NumeroSocio" IN ('1006', '1007') OR CAST("NumeroSocio" AS INTEGER) >= 1006;

-- Eliminar clientes de prueba (ajustar nombres según QA local)
DELETE FROM "Clientes"
WHERE "Nombre" ILIKE '%test%' OR "Apellido" ILIKE '%test%' OR "Nombre" ILIKE '%qa%';

COMMIT;

-- Si la base está vacía de datos reales, alternativa: reset completo (SOLO entorno nuevo):
-- docker compose down -v
