-- Crea la base de datos que usa Keycloak, separada de la base de la aplicación
-- (EventosVivosDb). Este script solo se ejecuta automáticamente cuando el volumen de
-- Postgres se inicializa por primera vez (volumen vacío). En un volumen ya existente
-- hay que crearla manualmente:
--   docker exec eventosvivos-db psql -U postgres -c 'CREATE DATABASE keycloak;'
SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec
