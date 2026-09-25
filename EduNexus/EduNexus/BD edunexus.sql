use Edunexus;


-- =========================================================
-- 1. ROLES
-- =========================================================

CREATE TABLE roles (
    id_rol VARCHAR(128) NOT NULL,
    nombre VARCHAR(256) NOT NULL,

    PRIMARY KEY (id_rol)
);

-- =========================================================
-- 2. USUARIOS
-- =========================================================

CREATE TABLE usuarios (
    id_usuario VARCHAR(128) NOT NULL,
    email VARCHAR(256) NOT NULL,
    nombre VARCHAR(256) NOT NULL,
    apellido1 VARCHAR(256) NOT NULL,
    apellido2 VARCHAR(256) ,
    identificacion VARCHAR(128) NOT NULL,
    estado BOOLEAN NOT NULL,
    telefono VARCHAR(128) NOT NULL,
    rol VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_usuario),

    CONSTRAINT fk_usuarios_rol
        FOREIGN KEY (rol)
        REFERENCES roles(id_rol)
);

-- =========================================================
-- 3. ADECUACIONES
-- =========================================================

CREATE TABLE adecuaciones (
    id_adecuacion INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,

    PRIMARY KEY (id_adecuacion)
);

-- =========================================================
-- 4. DISCAPACIDADES
-- =========================================================

CREATE TABLE discapacidades (
    id_discapacidad INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,

    PRIMARY KEY (id_discapacidad)
);

-- =========================================================
-- 5. SECCIONES
-- =========================================================

CREATE TABLE secciones (
    id_seccion INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,
    grado INT NOT NULL,
    cupo INT NOT NULL,

    PRIMARY KEY (id_seccion)
);

-- =========================================================
-- 6. MATERIA
-- =========================================================

CREATE TABLE materia (
    id_materia INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,
    profesor VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_materia),

    CONSTRAINT fk_materia_profesor
        FOREIGN KEY (profesor)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 7. PREMATRICULA
-- =========================================================

CREATE TABLE prematricula (
    id_matricula INT NOT NULL AUTO_INCREMENT,
    identificacion_estudiante INT NOT NULL,
    nombre VARCHAR(256) NOT NULL,
    apellido1 VARCHAR(256) NOT NULL,
    apellido2 VARCHAR(256) NOT NULL,
    grado VARCHAR(100) NOT NULL,
    genero VARCHAR(100) NOT NULL,
    telefono VARCHAR(100) NOT NULL,
    fecha_de_nacimiento DATE NOT NULL,
    fk_adecuacion INT NOT NULL,
    fk_discapacidad INT NOT NULL,
    fk_provincia_de_nacimiento INT NOT NULL,
    fk_distrito INT NOT NULL,
    direccion VARCHAR(256) NOT NULL,
    estado VARCHAR(50) NOT NULL,

    PRIMARY KEY (id_matricula),

    CONSTRAINT fk_prematricula_adecuacion
        FOREIGN KEY (fk_adecuacion)
        REFERENCES adecuaciones(id_adecuacion),

    CONSTRAINT fk_prematricula_discapacidad
        FOREIGN KEY (fk_discapacidad)
        REFERENCES discapacidades(id_discapacidad)
);

-- =========================================================
-- 8. ADECUACIONES_ESTUDIANTES
-- =========================================================

CREATE TABLE adecuaciones_estudiantes (
    id_adecuacion_estudiante INT NOT NULL AUTO_INCREMENT,
    fk_id_adecuacion INT NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_adecuacion_estudiante),

    CONSTRAINT fk_ae_adecuacion
        FOREIGN KEY (fk_id_adecuacion)
        REFERENCES adecuaciones(id_adecuacion),

    CONSTRAINT fk_ae_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 9. DISCAPACIDADES_ESTUDIANTES
-- =========================================================

CREATE TABLE discapacidades_estudiantes (
    id_discapacidad_estudiante INT NOT NULL AUTO_INCREMENT,
    fk_id_discapacidad INT NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_discapacidad_estudiante),

    CONSTRAINT fk_de_discapacidad
        FOREIGN KEY (fk_id_discapacidad)
        REFERENCES discapacidades(id_discapacidad),

    CONSTRAINT fk_de_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 10. EXPEDIENTE_ESTUDIANTES
-- =========================================================

CREATE TABLE expediente_estudiantes (
    id_expediente INT NOT NULL AUTO_INCREMENT,
    fk_id_matricula INT NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_expediente),

    CONSTRAINT fk_expediente_matricula
        FOREIGN KEY (fk_id_matricula)
        REFERENCES prematricula(id_matricula),

    CONSTRAINT fk_expediente_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 11. JUSTIFICACIONES
-- =========================================================

CREATE TABLE justificaciones (
    id_justificacion INT NOT NULL AUTO_INCREMENT,
    motivo VARCHAR(256) NOT NULL,
    dia DATE NOT NULL,
    hora_inicio DATETIME NOT NULL,
    hora_fin DATETIME NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_justificacion),

    CONSTRAINT fk_justificaciones_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 12. HORARIOS
-- =========================================================

CREATE TABLE horarios (
    id_horario INT NOT NULL AUTO_INCREMENT,
    hora_inicio DATETIME NOT NULL,
    hora_fin DATETIME NOT NULL,
    dia_semana VARCHAR(20) NOT NULL,
    fk_seccion INT NOT NULL,
    fk_materia INT NOT NULL,

    PRIMARY KEY (id_horario),

    CONSTRAINT fk_horarios_seccion
        FOREIGN KEY (fk_seccion)
        REFERENCES secciones(id_seccion),

    CONSTRAINT fk_horarios_materia
        FOREIGN KEY (fk_materia)
        REFERENCES materia(id_materia)
);

-- =========================================================
-- 13. CALENDARIO
-- =========================================================

CREATE TABLE calendario (
    id_calendario INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(256) NOT NULL,
    descripcion VARCHAR(512) NOT NULL,
    fecha_inicio DATE NOT NULL,
    fecha_fin DATE NOT NULL,

    PRIMARY KEY (id_calendario)
);

-- =========================================================
-- 14. INVENTARIO_COMEDOR
-- =========================================================

CREATE TABLE inventario_comedor (
    id_utencilio INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(100) NOT NULL,
    cantidad INT NOT NULL,
    cantidad_minima INT NOT NULL,

    PRIMARY KEY (id_utencilio)
);

-- =========================================================
-- 15. ASIGNACIONES
-- =========================================================

CREATE TABLE asignaciones (
    id_asignacion INT NOT NULL AUTO_INCREMENT,
    nombre VARCHAR(256) NOT NULL,
    descripcion VARCHAR(512) NOT NULL,
    porcentaje_nota DECIMAL(5,2) NOT NULL,
    calificacion DECIMAL(5,2) NOT NULL,
    estado VARCHAR(50) NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,
    fk_id_seccion INT NOT NULL,

    PRIMARY KEY (id_asignacion),

    CONSTRAINT fk_asignaciones_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario),

    CONSTRAINT fk_asignaciones_seccion
        FOREIGN KEY (fk_id_seccion)
        REFERENCES secciones(id_seccion)
);

-- =========================================================
-- 16. REPORTE_COMEDOR
-- =========================================================

CREATE TABLE reporte_comedor (
    id_reporte INT NOT NULL AUTO_INCREMENT,
    fecha DATE NOT NULL,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_reporte),

    CONSTRAINT fk_reporte_comedor_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);

-- =========================================================
-- 17. BITACORA
-- =========================================================

CREATE TABLE bitacora (
    id_evento INT NOT NULL AUTO_INCREMENT,
    tipo_de_evento VARCHAR(100) NOT NULL,
    descripcion_evento VARCHAR(512) NOT NULL,
    fecha DATETIME NOT NULL,
    datos_anteriores TEXT,
    datos_posteriores TEXT,
    fk_id_usuario VARCHAR(128) NOT NULL,

    PRIMARY KEY (id_evento),

    CONSTRAINT fk_bitacora_usuario
        FOREIGN KEY (fk_id_usuario)
        REFERENCES usuarios(id_usuario)
);



SHOW TABLES;