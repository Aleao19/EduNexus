-- =========================================================
-- EduNexus - Cambios de base de datos para el Sprint 1
-- Historias: RSEG-01-001/002/003, RCONF-04-001, RCONF-04-002, RGRU-06-003
--
-- Hecho sobre la estructura REAL de la base en Aiven.
-- - NO borra ni modifica tablas o columnas existentes.
-- - Solo crea UNA tabla nueva y agrega datos que falten.
-- - Se puede ejecutar más de una vez sin error ni duplicados.
-- =========================================================

USE Edunexus;

-- ---------------------------------------------------------
-- RGRU-06-003  Estudiantes asignados a cada sección (tabla nueva)
-- Permite contar estudiantes por grupo, calcular cupos disponibles
-- e impedir eliminar secciones con estudiantes (RCONF-04-002 #7).
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS secciones_estudiantes (
    id_seccion_estudiante INT NOT NULL AUTO_INCREMENT,
    fk_id_seccion INT NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,
    fecha_asignacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id_seccion_estudiante),
    UNIQUE KEY uq_seccion_estudiante (fk_id_seccion, fk_id_usuario),

    CONSTRAINT fk_se_seccion
        FOREIGN KEY (fk_id_seccion)
        REFERENCES secciones(id_seccion),

    CONSTRAINT fk_se_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- ---------------------------------------------------------
-- RSEG-01-001  Roles de la historia (solo los que falten)
-- ---------------------------------------------------------
INSERT INTO roles (id_rol, nombre)
SELECT UUID(), r.nombre
FROM (SELECT 'Administrador' AS nombre UNION ALL SELECT 'Director'
      UNION ALL SELECT 'Docente' UNION ALL SELECT 'Padre' UNION ALL SELECT 'Estudiante') r
WHERE NOT EXISTS (SELECT 1 FROM roles x WHERE x.nombre = r.nombre);

-- ---------------------------------------------------------
-- Administrador inicial (solo si no existe ese correo)
-- Correo: pedro.araya@mep.go.cr   Contraseña: Admin123  (cámbienla después)
-- ---------------------------------------------------------
INSERT INTO usuarios (id_usuario, email, nombre, apellido1, apellido2, identificacion,
                      estado, telefono, rol, password_hash, security_stamp, email_confirmado)
SELECT UUID(), 'pedro.araya@mep.go.cr', 'Pedro', 'Araya', 'Mora', '1-1111-1111',
       1, NULL,
       (SELECT id_rol FROM roles WHERE nombre = 'Administrador' LIMIT 1),
       'PBKDF2$10000$16x3xjxq40YS9bj6uvKSwQ==$94DYf+UIHeIulZh2wkwUYrqum9oA/wGcj9ECeU+sNqQ=',
       UUID(), 1
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE email = 'pedro.araya@mep.go.cr');

-- ---------------------------------------------------------
-- RCONF-04-002  Periodo lectivo del año actual en el calendario
-- (las secciones se ligan al calendario; solo se crea si no hay
--  ningún periodo que inicie este año)
-- ---------------------------------------------------------
INSERT INTO calendario (nombre, descripcion, fecha_inicio, fecha_fin)
SELECT CONCAT('Curso lectivo ', YEAR(CURDATE())), 'Periodo lectivo',
       MAKEDATE(YEAR(CURDATE()), 1), DATE(CONCAT(YEAR(CURDATE()), '-12-31'))
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM calendario WHERE YEAR(fecha_inicio) = YEAR(CURDATE()));

-- Verificación rápida
SELECT 'roles' AS tabla, COUNT(*) AS filas FROM roles
UNION ALL SELECT 'usuarios', COUNT(*) FROM usuarios
UNION ALL SELECT 'grados', COUNT(*) FROM grados
UNION ALL SELECT 'calendario', COUNT(*) FROM calendario
UNION ALL SELECT 'secciones', COUNT(*) FROM secciones
UNION ALL SELECT 'secciones_estudiantes', COUNT(*) FROM secciones_estudiantes;
