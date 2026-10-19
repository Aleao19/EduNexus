-- =========================================================
-- EduNexus - Ajuste de tablas para RMPR-08-001 (Prematrículas)
--
-- Adapta las tablas EXISTENTES prematricula y expediente_estudiantes
-- a lo que pide el formulario de prematrícula.
-- - No borra tablas, columnas ni datos.
-- - Se puede ejecutar más de una vez sin error.
-- - Ejecutar completo en MySQL Workbench (usa DELIMITER).
-- =========================================================

USE Edunexus;

DROP PROCEDURE IF EXISTS en_agregar_columna;
DROP PROCEDURE IF EXISTS en_agregar_unico;

DELIMITER $$

CREATE PROCEDURE en_agregar_columna(IN p_tabla VARCHAR(64), IN p_columna VARCHAR(64), IN p_definicion VARCHAR(512))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tabla AND COLUMN_NAME = p_columna
    ) THEN
        SET @sql_en = CONCAT('ALTER TABLE `', p_tabla, '` ADD COLUMN `', p_columna, '` ', p_definicion);
        PREPARE st_en FROM @sql_en;
        EXECUTE st_en;
        DEALLOCATE PREPARE st_en;
    END IF;
END$$

CREATE PROCEDURE en_agregar_unico(IN p_tabla VARCHAR(64), IN p_nombre VARCHAR(64), IN p_columnas VARCHAR(256))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_tabla AND INDEX_NAME = p_nombre
    ) THEN
        SET @sql_en = CONCAT('ALTER TABLE `', p_tabla, '` ADD UNIQUE KEY `', p_nombre, '` (', p_columnas, ')');
        PREPARE st_en FROM @sql_en;
        EXECUTE st_en;
        DEALLOCATE PREPARE st_en;
    END IF;
END$$

DELIMITER ;

-- ---------------------------------------------------------
-- prematricula
-- ---------------------------------------------------------
-- La identificación es texto (ej: 1-1234-5678), no número
ALTER TABLE prematricula MODIFY identificacion_estudiante VARCHAR(50) NOT NULL;

-- Datos que el formulario permite dejar vacíos
ALTER TABLE prematricula MODIFY apellido2 VARCHAR(256) NULL;
ALTER TABLE prematricula MODIFY genero VARCHAR(100) NULL;
ALTER TABLE prematricula MODIFY telefono VARCHAR(100) NULL;
ALTER TABLE prematricula MODIFY direccion VARCHAR(256) NULL;

-- Datos que el formulario no pide (se conservan como opcionales)
ALTER TABLE prematricula MODIFY fk_adecuacion INT NULL;
ALTER TABLE prematricula MODIFY fk_discapacidad INT NULL;
ALTER TABLE prematricula MODIFY fk_provincia_de_nacimiento INT NULL;
ALTER TABLE prematricula MODIFY fk_distrito INT NULL;

-- Estado de la prematrícula: por defecto "Pendiente"
ALTER TABLE prematricula MODIFY estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente';

-- Columnas que pide el formulario
CALL en_agregar_columna('prematricula', 'tiene_adecuacion', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL en_agregar_columna('prematricula', 'tiene_discapacidad', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL en_agregar_columna('prematricula', 'contacto_emergencia', 'VARCHAR(256) NULL');
CALL en_agregar_columna('prematricula', 'es_nuevo_ingreso', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL en_agregar_columna('prematricula', 'fecha_prematricula', 'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP');

-- No se permiten dos prematrículas con la misma identificación
CALL en_agregar_unico('prematricula', 'uq_prematricula_identificacion', 'identificacion_estudiante');

-- ---------------------------------------------------------
-- expediente_estudiantes
-- ---------------------------------------------------------
-- Un estudiante de nuevo ingreso todavía no tiene usuario en el sistema
ALTER TABLE expediente_estudiantes MODIFY fk_id_usuario VARCHAR(128) NULL;

DROP PROCEDURE IF EXISTS en_agregar_columna;
DROP PROCEDURE IF EXISTS en_agregar_unico;

-- Verificación
SHOW COLUMNS FROM prematricula;
SHOW COLUMNS FROM expediente_estudiantes;
