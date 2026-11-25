using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace EduNexus.UI.Models.Identity
{
    public class EduNexusDbContext : DbContext
    {
        static EduNexusDbContext()
        {
            // La base de datos ya existe en Aiven.
            // Entity Framework no debe crearla
            // ni modificarla automáticamente.
            Database.SetInitializer<EduNexusDbContext>(null);
        }

        public EduNexusDbContext()
            : base("name=EduNexusContext")
        {
        }

        // ========================================
        // TABLAS EXISTENTES DEL PROYECTO
        // ========================================

        public DbSet<UsuarioEntity> Usuarios { get; set; }

        public DbSet<RolEntity> Roles { get; set; }

        public DbSet<GradoEntity> Grados { get; set; }

        public DbSet<SeccionEntity> Secciones { get; set; }

        public DbSet<CalendarioEntity> Calendarios { get; set; }

        public DbSet<BitacoraEntity> Bitacora { get; set; }

        public DbSet<SeccionEstudianteEntity>
            SeccionesEstudiantes { get; set; }

        // ========================================
        // EXPEDIENTES ESTUDIANTILES
        // ========================================

        public DbSet<ExpedienteEntity> Expedientes { get; set; }

        // ========================================
        // PREMATRÍCULAS
        // ========================================

        public DbSet<PrematriculaEntity> Prematriculas { get; set; }

        // ========================================
        // MATERIAS Y SECCIONES
        // ========================================

        public DbSet<MateriaEntity> Materias { get; set; }

        public DbSet<SeccionMateriaEntity>
            SeccionesMaterias { get; set; }

        // ========================================
        // ASISTENCIA
        // ========================================

        public DbSet<AsistenciaEntity> Asistencias { get; set; }

        // ========================================
        // RASI-10-004
        // NOTIFICACIONES DE AUSENCIA
        // ========================================

        public DbSet<NotificacionEntity>
            Notificaciones { get; set; }

        public DbSet<EncargadoEstudianteEntity>
            EncargadosEstudiantes { get; set; }

        // ========================================
        // RASI-10-004
        // JUSTIFICACIONES DE AUSENCIA
        // ========================================

        public DbSet<JustificacionAusenciaEntity>
            JustificacionesAusencia { get; set; }

        // ========================================
        // RJT-11-002
        // REGISTRO DE SALIDAS DE ESTUDIANTES
        // ========================================

        public DbSet<SalidaEstudianteEntity>
            SalidasEstudiantes { get; set; }

        // ========================================
        // REGISTRO DE NOTAS
        // ========================================

        public DbSet<NotaEntity> Notas { get; set; }

        // ========================================
        // CONFIGURACIÓN DE ENTIDADES
        // ========================================

        protected override void OnModelCreating(
            DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================
            // USUARIOS
            // ========================================

            modelBuilder.Entity<UsuarioEntity>()
                .ToTable("usuarios")
                .HasKey(u => u.id_usuario);

            // ========================================
            // ROLES
            // ========================================

            modelBuilder.Entity<RolEntity>()
                .ToTable("roles")
                .HasKey(r => r.id_rol);

            // ========================================
            // GRADOS
            // ========================================

            modelBuilder.Entity<GradoEntity>()
                .ToTable("grados")
                .HasKey(g => g.id_grado);

            modelBuilder.Entity<GradoEntity>()
                .Property(g => g.id_grado)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // SECCIONES
            // ========================================

            modelBuilder.Entity<SeccionEntity>()
                .ToTable("secciones")
                .HasKey(s => s.id_seccion);

            modelBuilder.Entity<SeccionEntity>()
                .Property(s => s.id_seccion)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // CALENDARIO
            // ========================================

            modelBuilder.Entity<CalendarioEntity>()
                .ToTable("calendario")
                .HasKey(c => c.id_calendario);

            // ========================================
            // BITÁCORA
            // ========================================

            modelBuilder.Entity<BitacoraEntity>()
                .ToTable("bitacora")
                .HasKey(b => b.id_evento);

            modelBuilder.Entity<BitacoraEntity>()
                .Property(b => b.id_evento)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // SECCIONES ESTUDIANTES
            // ========================================

            modelBuilder.Entity<SeccionEstudianteEntity>()
                .ToTable("secciones_estudiantes")
                .HasKey(se => se.id_seccion_estudiante);

            modelBuilder.Entity<SeccionEstudianteEntity>()
                .Property(se => se.id_seccion_estudiante)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // EXPEDIENTES ESTUDIANTILES
            // ========================================

            modelBuilder.Entity<ExpedienteEntity>()
                .ToTable("expediente_estudiantes")
                .HasKey(e => e.id_expediente);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.id_expediente)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.fk_id_usuario)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.fk_id_matricula)
                .IsRequired();

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.correo)
                .HasMaxLength(256);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.grupo_sanguineo)
                .HasMaxLength(10);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.contactoEmergencia)
                .HasMaxLength(256);

            // ========================================
            // PREMATRÍCULA
            // ========================================

            modelBuilder.Entity<PrematriculaEntity>()
                .ToTable("prematricula")
                .HasKey(p => p.id_prematricula);

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.id_prematricula)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.identificacion)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.nombre)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.apellido1)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.apellido2)
                .HasMaxLength(100)
                .IsOptional();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.genero)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.telefono)
                .HasMaxLength(30)
                .IsOptional();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.contactoEmergencia)
                .HasMaxLength(150)
                .IsOptional();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.direccion)
                .HasMaxLength(500)
                .IsOptional();

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.estado)
                .HasMaxLength(50)
                .IsRequired();

            // ========================================
            // RMPR-08-007
            // PERÍODO LECTIVO DE PREMATRÍCULA
            // Y RENOVACIÓN ANUAL
            // ========================================

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.fk_id_calendario)
                .HasColumnName("fk_id_calendario")
                .IsOptional();

            // ========================================
            // MATERIAS
            // ========================================

            modelBuilder.Entity<MateriaEntity>()
                .ToTable("materias")
                .HasKey(m => m.id_materia);

            modelBuilder.Entity<MateriaEntity>()
                .Property(m => m.id_materia)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // SECCIONES MATERIAS
            // Mapeo provisional: verificar clave
            // primaria con la entidad y MySQL.
            // ========================================

            modelBuilder.Entity<SeccionMateriaEntity>()
                .ToTable("secciones_materias")
                .HasKey(sm => new
                {
                    sm.fk_id_seccion,
                    sm.fk_id_materia
                });

            // ========================================
            // ASISTENCIA
            // ========================================

            modelBuilder.Entity<AsistenciaEntity>()
                .ToTable("asistencia")
                .HasKey(a => a.id_asistencia);

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.id_asistencia)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.estado)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.fk_id_usuario)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.fk_id_registrado_por)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.motivo_justificacion)
                .HasMaxLength(500)
                .IsOptional();

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.fk_id_justificado_por)
                .HasMaxLength(128)
                .IsOptional();

            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.fecha_justificacion)
                .IsOptional();

            // ========================================
            // RASI-10-004
            // NOTIFICACIONES
            // ========================================

            modelBuilder.Entity<NotificacionEntity>()
                .ToTable("notificaciones")
                .HasKey(n => n.id_notificacion);

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.id_notificacion)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.fk_id_asistencia)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.fk_id_estudiante)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.fk_id_destinatario)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.titulo)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.mensaje)
                .HasMaxLength(1000)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.fecha_creacion)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.leida)
                .IsRequired();

            modelBuilder.Entity<NotificacionEntity>()
                .Property(n => n.fecha_lectura)
                .IsOptional();

            // ========================================
            // RASI-10-004
            // RELACIÓN ENCARGADO - ESTUDIANTE
            // ========================================

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .ToTable("encargados_estudiantes")
                .HasKey(e => e.id_encargado_estudiante);

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.id_encargado_estudiante)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.fk_id_encargado)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.fk_id_estudiante)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.parentesco)
                .HasMaxLength(50)
                .IsOptional();

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.estado)
                .IsRequired();

            modelBuilder.Entity<EncargadoEstudianteEntity>()
                .Property(e => e.fecha_registro)
                .IsRequired();

            // ========================================
            // RASI-10-004
            // JUSTIFICACIONES DE AUSENCIA
            // ========================================

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .ToTable("justificaciones_ausencia")
                .HasKey(j => j.id_justificacion);

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.id_justificacion)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fk_id_asistencia)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fk_id_estudiante)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fk_id_encargado)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.motivo)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fecha_envio)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.estado)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fk_id_docente)
                .HasMaxLength(128)
                .IsOptional();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.fecha_revision)
                .IsOptional();

            modelBuilder.Entity<JustificacionAusenciaEntity>()
                .Property(j => j.observaciones)
                .HasMaxLength(500)
                .IsOptional();

            // ========================================
            // RJT-11-002
            // REGISTRO DE SALIDAS DE ESTUDIANTES
            // ========================================

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .ToTable("salidas_estudiantes")
                .HasKey(s => s.id_salida);

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.id_salida)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.fk_id_estudiante)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.fk_id_seccion)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.fecha_salida)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.motivo)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.nombre_persona_retira)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.identificacion_persona_retira)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.parentesco)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.telefono_persona_retira)
                .HasMaxLength(30)
                .IsOptional();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.fk_id_registrado_por)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.fecha_registro)
                .IsRequired();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.observaciones)
                .HasMaxLength(500)
                .IsOptional();

            modelBuilder.Entity<SalidaEstudianteEntity>()
                .Property(s => s.estado)
                .HasMaxLength(30)
                .IsRequired();

            // ========================================
            // REGISTRO DE NOTAS
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .ToTable("notas")
                .HasKey(n => n.id_nota);

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.id_nota)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.Identity);

            // ========================================
            // INFORMACIÓN DEL ESTUDIANTE
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_estudiante)
                .HasMaxLength(128)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_seccion)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_materia)
                .IsRequired();

            // ========================================
            // INFORMACIÓN DEL DOCENTE
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_docente)
                .HasMaxLength(128)
                .IsRequired();

            // ========================================
            // CALENDARIO Y PERÍODO ACADÉMICO
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_calendario)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.periodo)
                .HasMaxLength(50)
                .IsRequired();

            // ========================================
            // INFORMACIÓN DE LA EVALUACIÓN
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.nombre_evaluacion)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.tipo_evaluacion)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fecha_evaluacion)
                .IsRequired();

            // ========================================
            // CALIFICACIÓN DEL ESTUDIANTE
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.nota)
                .HasPrecision(5, 2)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.observaciones)
                .HasMaxLength(500)
                .IsOptional();

            // ========================================
            // CONTROL DEL REGISTRO
            // ========================================

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fecha_registro)
                .IsRequired();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fecha_modificacion)
                .IsOptional();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.fk_id_modificado_por)
                .HasMaxLength(128)
                .IsOptional();

            modelBuilder.Entity<NotaEntity>()
                .Property(n => n.estado)
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}