
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace EduNexus.UI.Models.Identity
{
    public class EduNexusDbContext : DbContext
    {
        static EduNexusDbContext()
        {
            // La base ya existe en Aiven: Entity Framework no debe crearla ni modificarla.
            Database.SetInitializer<EduNexusDbContext>(null);
        }

        public EduNexusDbContext() : base("name=EduNexusContext") { }

        public DbSet<UsuarioEntity> Usuarios { get; set; }
        public DbSet<RolEntity> Roles { get; set; }
        public DbSet<GradoEntity> Grados { get; set; }
        public DbSet<SeccionEntity> Secciones { get; set; }
        public DbSet<CalendarioEntity> Calendarios { get; set; }
        public DbSet<BitacoraEntity> Bitacora { get; set; }
        public DbSet<SeccionEstudianteEntity> SeccionesEstudiantes { get; set; }
        public DbSet<MateriaEntity> Materias { get; set; }
        public DbSet<SeccionMateriaEntity> SeccionesMaterias { get; set; }
        public DbSet<HorarioEntity> Horarios { get; set; }

        // RMPR-08-001: entidades para registrar prematrículas y expedientes.
        public DbSet<PrematriculaEntity> Prematriculas { get; set; }
        public DbSet<ExpedienteEntity> Expedientes { get; set; }

        // REXP-03-002: documentos adjuntos al expediente (tabla documentos_expediente)
        public DbSet<DocumentoExpedienteEntity> DocumentosExpediente { get; set; }

        // REXP-02-007 / 008 / 009: expediente académico
        public DbSet<AdecuacionEntity> Adecuaciones { get; set; }
        public DbSet<AdecuacionEstudianteEntity> AdecuacionesEstudiantes { get; set; }
        public DbSet<TipoAyudaEntity> TiposAyuda { get; set; }
        public DbSet<AyudaEstudianteEntity> AyudasEstudiantes { get; set; }
        public DbSet<AsignacionEntity> Asignaciones { get; set; }
        //RASI-10-001: Asistencia de estudiantes
        public DbSet<AsistenciaEntity> Asistencias { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioEntity>()
                .ToTable("usuarios")
                .HasKey(u => u.id_usuario);

            modelBuilder.Entity<RolEntity>()
                .ToTable("roles")
                .HasKey(r => r.id_rol);

            modelBuilder.Entity<GradoEntity>()
                .ToTable("grados")
                .HasKey(g => g.id_grado);

            modelBuilder.Entity<GradoEntity>()
                .Property(g => g.id_grado)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SeccionEntity>()
                .ToTable("secciones")
                .HasKey(s => s.id_seccion);

            modelBuilder.Entity<SeccionEntity>()
                .Property(s => s.id_seccion)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<CalendarioEntity>()
                .ToTable("calendario")
                .HasKey(c => c.id_calendario);

            modelBuilder.Entity<BitacoraEntity>()
                .ToTable("bitacora")
                .HasKey(b => b.id_evento);

            modelBuilder.Entity<BitacoraEntity>()
                .Property(b => b.id_evento)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SeccionEstudianteEntity>()
                .ToTable("secciones_estudiantes")
                .HasKey(se => se.id_seccion_estudiante);

            modelBuilder.Entity<SeccionEstudianteEntity>()
                .Property(se => se.id_seccion_estudiante)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<MateriaEntity>()
                .ToTable("materia")
                .HasKey(m => m.id_materia);

            modelBuilder.Entity<MateriaEntity>()
                .Property(m => m.id_materia)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SeccionMateriaEntity>()
                .ToTable("secciones_materias")
                .HasKey(sm => sm.id_seccion_materia);

            modelBuilder.Entity<SeccionMateriaEntity>()
                .Property(sm => sm.id_seccion_materia)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<HorarioEntity>()
                .ToTable("horarios")
                .HasKey(h => h.id_horario);

            modelBuilder.Entity<HorarioEntity>()
                .Property(h => h.id_horario)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // =========================================================
            // RMPR-08-001: PREMATRÍCULAS -> tabla real "prematricula"
            // (ajustada con el script "BD sprint2 prematriculas.sql")
            // =========================================================
            modelBuilder.Entity<PrematriculaEntity>()
                .ToTable("prematricula")
                .HasKey(p => p.id_prematricula);

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.id_prematricula)
                .HasColumnName("id_matricula")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.identificacion)
                .HasColumnName("identificacion_estudiante");

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.fecha_nacimiento)
                .HasColumnName("fecha_de_nacimiento");

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.contactoEmergencia)
                .HasColumnName("contacto_emergencia");

            // =========================================================
            // RMPR-08-001: EXPEDIENTES -> tabla real "expediente_estudiantes"
            // El expediente queda ligado a la prematrícula (fk_id_matricula);
            // los datos personales se leen de la prematrícula y aquí solo
            // se guardan correo, datos médicos y contacto de emergencia.
            // =========================================================
            modelBuilder.Entity<ExpedienteEntity>()
                .ToTable("expediente_estudiantes")
                .HasKey(e => e.id_expediente);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.id_expediente)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);


            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.contactoEmergencia)
                .HasColumnName("contacto_emergencia");

            // =========================================================
            // REXP-03-002: DOCUMENTOS DEL EXPEDIENTE
            // -> tabla "documentos_expediente"
            // =========================================================
            modelBuilder.Entity<DocumentoExpedienteEntity>()
                .ToTable("documentos_expediente")
                .HasKey(d => d.id_documento);

            modelBuilder.Entity<DocumentoExpedienteEntity>()
                .Property(d => d.id_documento)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // =========================================================
            // REXP-02-007 / 008 / 009: EXPEDIENTE ACADÉMICO
            // (tablas ajustadas o creadas con "BD sprint2 expedientes.sql")
            // =========================================================
            modelBuilder.Entity<AdecuacionEntity>()
                .ToTable("adecuaciones")
                .HasKey(a => a.id_adecuacion);
            modelBuilder.Entity<AdecuacionEntity>()
                .Property(a => a.id_adecuacion)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<AdecuacionEstudianteEntity>()
                .ToTable("adecuaciones_estudiantes")
                .HasKey(a => a.id_adecuacion_estudiante);
            modelBuilder.Entity<AdecuacionEstudianteEntity>()
                .Property(a => a.id_adecuacion_estudiante)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<TipoAyudaEntity>()
                .ToTable("tipos_ayuda")
                .HasKey(t => t.id_tipo_ayuda);
            modelBuilder.Entity<TipoAyudaEntity>()
                .Property(t => t.id_tipo_ayuda)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<AyudaEstudianteEntity>()
                .ToTable("ayudas_estudiantes")
                .HasKey(a => a.id_ayuda_estudiante);
            modelBuilder.Entity<AyudaEstudianteEntity>()
                .Property(a => a.id_ayuda_estudiante)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<AsignacionEntity>()
                .ToTable("asignaciones")
                .HasKey(a => a.id_asignacion);
            modelBuilder.Entity<AsignacionEntity>()
                .Property(a => a.id_asignacion)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            //RASI-10-001: Asistencia de estudiantes
            modelBuilder.Entity<AsistenciaEntity>()
                .ToTable("asistencia")
                .HasKey(a => a.id_asistencia);
            modelBuilder.Entity<AsistenciaEntity>()
                .Property(a => a.id_asistencia)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // NOTA: esta configuración no crea tablas ni comprueba
            // que existan en Aiven. El inicializador está deshabilitado.
            // Antes de probar el guardado, verificar el esquema real.

            base.OnModelCreating(modelBuilder);
        }
    }
}
