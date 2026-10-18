
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
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

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
            // RMPR-08-001: PREMATRÍCULAS
            // PENDIENTE: cuando se recupere el acceso a Aiven:
            // 1. Confirmar el nombre real de la tabla.
            // 2. Verificar las columnas y sus tipos de datos.
            // 3. Confirmar que id_prematricula sea la clave primaria.
            // 4. Confirmar si la clave primaria es autogenerada.
            // Si el esquema real difiere, ajustar esta configuración
            // y PrematriculaEntity.cs.
            // =========================================================
            modelBuilder.Entity<PrematriculaEntity>()
                .ToTable("prematriculas")
                .HasKey(p => p.id_prematricula);

            modelBuilder.Entity<PrematriculaEntity>()
                .Property(p => p.id_prematricula)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // =========================================================
            // RMPR-08-001: EXPEDIENTES
            // PENDIENTE: cuando se recupere el acceso a Aiven:
            // 1. Confirmar el nombre real de la tabla.
            // 2. Verificar las columnas y sus tipos de datos.
            // 3. Confirmar la clave primaria y si es autogenerada.
            // 4. Verificar cómo se relaciona el expediente con la
            //    prematrícula y si existe una clave foránea.
            // 5. Ajustar esta configuración y ExpedienteEntity.cs
            //    según el esquema real de la base de datos.
            // =========================================================
            modelBuilder.Entity<ExpedienteEntity>()
                .ToTable("expedientes")
                .HasKey(e => e.id_expediente);

            modelBuilder.Entity<ExpedienteEntity>()
                .Property(e => e.id_expediente)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // NOTA: esta configuración no crea tablas ni comprueba
            // que existan en Aiven. El inicializador está deshabilitado.
            // Antes de probar el guardado, verificar el esquema real.

            base.OnModelCreating(modelBuilder);
        }
    }
}
