using System.Data.Entity;
using System.Xml;

namespace EduNexus.UI.Models.Identity
{
    public class EduNexusDbContext : DbContext
    {
        public EduNexusDbContext() : base("name=EduNexusContext") { }

        public DbSet<UsuarioEntity> Usuarios { get; set; }
        public DbSet<RolEntity> Roles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioEntity>().ToTable("usuarios").HasKey(u => u.id_usuario);
            modelBuilder.Entity<RolEntity>().ToTable("roles").HasKey(r => r.id_rol);
        }
    }
}