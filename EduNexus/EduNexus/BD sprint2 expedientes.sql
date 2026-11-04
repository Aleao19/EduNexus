-- =========================================================
-- EduNexus - Cambios de base de datos para el expediente académico
-- Historias: REXP-02-007, REXP-02-008, REXP-02-009
--
-- - No borra tablas, columnas ni datos.
-- - Se puede ejecutar más de una vez sin error ni duplicados.
-- - Ejecutar completo en MySQL Workbench (usa DELIMITER).
-- =========================================================

USE Edunexus;
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS en_agregar_columna;
DROP PROCEDURE IF EXISTS en_agregar_unico;

DELIMITER $$
CREATE PROCEDURE en_agregar_columna(IN p_tabla VARCHAR(64), IN p_columna VARCHAR(64), IN p_definicion VARCHAR(512))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tabla AND COLUMN_NAME = p_columna) THEN
        SET @sql_en = CONCAT('ALTER TABLE `', p_tabla, '` ADD COLUMN `', p_columna, '` ', p_definicion);
        PREPARE st_en FROM @sql_en; EXECUTE st_en; DEALLOCATE PREPARE st_en;
    END IF;
END$$
CREATE PROCEDURE en_agregar_unico(IN p_tabla VARCHAR(64), IN p_nombre VARCHAR(64), IN p_columnas VARCHAR(256))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
                   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tabla AND INDEX_NAME = p_nombre) THEN
        SET @sql_en = CONCAT('ALTER TABLE `', p_tabla, '` ADD UNIQUE KEY `', p_nombre, '` (', p_columnas, ')');
        PREPARE st_en FROM @sql_en; EXECUTE st_en; DEALLOCATE PREPARE st_en;
    END IF;
END$$
DELIMITER ;

-- ---------------------------------------------------------
-- REXP-02-008  Tipos de adecuación (catálogo) y casos por estudiante
-- ---------------------------------------------------------
CALL en_agregar_columna('adecuaciones', 'descripcion', 'VARCHAR(512) NULL');
CALL en_agregar_unico('adecuaciones', 'uq_adecuaciones_nombre', 'nombre');

INSERT INTO adecuaciones (nombre, descripcion)
SELECT t.nombre, t.descripcion
FROM (SELECT 'Adecuación curricular significativa' AS nombre,
             'Modifica los objetivos y contenidos del programa de estudio.' AS descripcion
      UNION ALL
      SELECT 'Adecuación curricular no significativa',
             'Ajusta la metodología y la evaluación sin modificar los objetivos del programa.') t
WHERE NOT EXISTS (SELECT 1 FROM adecuaciones a WHERE a.nombre = t.nombre);

-- Un caso puede registrarse "sin clasificar" y clasificarse después
ALTER TABLE adecuaciones_estudiantes MODIFY fk_id_adecuacion INT NULL;
CALL en_agregar_columna('adecuaciones_estudiantes', 'descripcion', 'VARCHAR(512) NULL');
CALL en_agregar_columna('adecuaciones_estudiantes', 'estado', 'VARCHAR(20) NOT NULL DEFAULT ''Activa''');
CALL en_agregar_columna('adecuaciones_estudiantes', 'fecha_registro', 'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP');

-- ---------------------------------------------------------
-- REXP-02-009  Tipos de ayuda (catálogo) y casos por estudiante (tablas nuevas)
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS tipos_ayuda (
    id_tipo_ayuda INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(512) NULL,
    PRIMARY KEY (id_tipo_ayuda),
    UNIQUE KEY uq_tipos_ayuda_nombre (nombre)
);

INSERT INTO tipos_ayuda (nombre, descripcion)
SELECT t.nombre, t.descripcion
FROM (SELECT 'Alimentaria' AS nombre, 'Servicio de comedor o apoyo alimentario.' AS descripcion
      UNION ALL SELECT 'Transporte', 'Apoyo para el traslado del estudiante al centro educativo.'
      UNION ALL SELECT 'Beca económica', 'Apoyo económico para gastos de estudio.'
      UNION ALL SELECT 'Útiles escolares', 'Entrega de materiales y útiles escolares.') t
WHERE NOT EXISTS (SELECT 1 FROM tipos_ayuda x WHERE x.nombre = t.nombre);

CREATE TABLE IF NOT EXISTS ayudas_estudiantes (
    id_ayuda_estudiante INT NOT NULL AUTO_INCREMENT,
    fk_id_usuario VARCHAR(128) NOT NULL,
    fk_id_tipo_ayuda INT NULL,
    descripcion VARCHAR(512) NOT NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'Activa',
    fecha_registro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id_ayuda_estudiante),
    CONSTRAINT fk_ae2_usuario FOREIGN KEY (fk_id_usuario) REFERENCES usuarios(id_usuario),
    CONSTRAINT fk_ae2_tipo FOREIGN KEY (fk_id_tipo_ayuda) REFERENCES tipos_ayuda(id_tipo_ayuda)
);

DROP PROCEDURE IF EXISTS en_agregar_columna;
DROP PROCEDURE IF EXISTS en_agregar_unico;

-- Verificación
SELECT 'adecuaciones' AS tabla, COUNT(*) AS filas FROM adecuaciones
UNION ALL SELECT 'adecuaciones_estudiantes', COUNT(*) FROM adecuaciones_estudiantes
UNION ALL SELECT 'tipos_ayuda', COUNT(*) FROM tipos_ayuda
UNION ALL SELECT 'ayudas_estudiantes', COUNT(*) FROM ayudas_estudiantes;
