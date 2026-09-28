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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioEntity>().ToTable("usuarios").HasKey(u => u.id_usuario);
            modelBuilder.Entity<RolEntity>().ToTable("roles").HasKey(r => r.id_rol);

            modelBuilder.Entity<GradoEntity>().ToTable("grados").HasKey(g => g.id_grado);
            modelBuilder.Entity<GradoEntity>().Property(g => g.id_grado)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SeccionEntity>().ToTable("secciones").HasKey(s => s.id_seccion);
            modelBuilder.Entity<SeccionEntity>().Property(s => s.id_seccion)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<CalendarioEntity>().ToTable("calendario").HasKey(c => c.id_calendario);

            modelBuilder.Entity<BitacoraEntity>().ToTable("bitacora").HasKey(b => b.id_evento);
            modelBuilder.Entity<BitacoraEntity>().Property(b => b.id_evento)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            modelBuilder.Entity<SeccionEstudianteEntity>().ToTable("secciones_estudiantes").HasKey(se => se.id_seccion_estudiante);
            modelBuilder.Entity<SeccionEstudianteEntity>().Property(se => se.id_seccion_estudiante)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }
}
