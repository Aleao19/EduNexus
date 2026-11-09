-- =========================================================
-- EduNexus - Sprint 3
-- La tabla materia tenía profesor y descripcion como NOT NULL,
-- pero en la pantalla de Materias ambos campos son opcionales.
-- Sin esto, crear una materia sin profesor o sin descripción da error.
-- =========================================================
ALTER TABLE materia
    MODIFY profesor VARCHAR(128) NULL,
    MODIFY descripcion VARCHAR(512) NULL;

-- =========================================================
-- Documentos del expediente (REXP-03-002): antes los archivos
-- no quedaban registrados en ningún lado.
-- =========================================================
CREATE TABLE documentos_expediente (
    id_documento INT AUTO_INCREMENT PRIMARY KEY,
    fk_id_expediente INT NOT NULL,
    nombre_documento VARCHAR(150) NOT NULL,
    descripcion VARCHAR(500) NULL,
    nombre_archivo VARCHAR(255) NOT NULL,
    nombre_interno VARCHAR(64) NOT NULL,
    tipo_archivo VARCHAR(100) NOT NULL,
    tamano_bytes INT NOT NULL,
    fecha_registro DATETIME NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,
    CONSTRAINT fk_doc_exp FOREIGN KEY (fk_id_expediente) REFERENCES expediente_estudiantes(id_expediente),
    CONSTRAINT fk_doc_usr FOREIGN KEY (fk_id_usuario) REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- Asistencia: un solo registro por estudiante, materia, sección y día.
-- =========================================================
ALTER TABLE asistencia
    ADD UNIQUE KEY uq_asistencia_estudiante_dia (fecha, fk_id_seccion, fk_id_materia, fk_id_usuario);

-- =========================================================
-- Roles: "Profesor" estaba repetido de "Docente".
-- Se pasan sus usuarios a Docente y se elimina el rol.
-- =========================================================
SET @docente  = (SELECT id_rol FROM roles WHERE nombre = 'Docente');
SET @profesor = (SELECT id_rol FROM roles WHERE nombre = 'Profesor');
UPDATE usuarios SET rol = @docente WHERE rol = @profesor;
DELETE FROM roles WHERE id_rol = @profesor;

-- =========================================================
-- Prematrícula: el índice único por identificación (sprint 2) impedía
-- renovar la matrícula (RMPR), porque la renovación crea una nueva
-- solicitud con la misma identificación para el año siguiente.
-- Ahora la identificación es única por período lectivo.
-- =========================================================
ALTER TABLE prematricula
    DROP INDEX uq_prematricula_identificacion,
    ADD UNIQUE KEY uq_prematricula_identificacion_periodo (identificacion_estudiante, fk_id_calendario);
