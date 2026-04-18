using Microsoft.EntityFrameworkCore;
using sigbu_mvc.Models;

namespace sigbu_mvc.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Area> Areas { get; set; } = null!;
        public DbSet<Bien> Bienes { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Solicitud> Solicitudes { get; set; } = null!;
        public DbSet<SolicitudAreaDetalle> SolicitudAreaDetalles { get; set; } = null!;
        public DbSet<SolicitudBienDetalle> SolicitudBienDetalles { get; set; } = null!;
        public DbSet<SolicitudTransferenciaDetalle> SolicitudTransferenciaDetalles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // convención global ->  tablas y columnas en minusculas
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName()!.ToLower());

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName()!.ToLower());
                }
            }

            // relaciones de bien
            modelBuilder.Entity<Bien>()
                .HasIndex(b => b.codigo)
                .IsUnique()
                .HasFilter("\"codigo\" IS NOT NULL");

            modelBuilder.Entity<Bien>()
                .HasIndex(b => b.serie)
                .IsUnique()
                .HasFilter("\"serie\" IS NOT NULL");

            modelBuilder.Entity<Bien>()
                .HasOne(b => b.Area)
                .WithMany()
                .HasForeignKey(b => b.area_id);

            // relaciones de solicitudes
            modelBuilder.Entity<SolicitudAreaDetalle>()
                .HasOne<Solicitud>()
                .WithMany(s => s.area_detalles)
                .HasForeignKey(d => d.solicitud_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SolicitudAreaDetalle>()
                .HasOne<Area>()
                .WithMany()
                .HasForeignKey(d => d.area_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SolicitudBienDetalle>()
                .HasOne<Solicitud>()
                .WithMany(s => s.bien_detalles)
                .HasForeignKey(d => d.solicitud_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SolicitudBienDetalle>()
                .HasOne(d => d.area)
                .WithMany()
                .HasForeignKey(d => d.area_id);

            // valores por defecto
            modelBuilder.Entity<Solicitud>()
                .Property(s => s.estado)
                .HasDefaultValue("pendiente");

            modelBuilder.Entity<Solicitud>()
                .Property(s => s.categoria)
                .HasDefaultValue("categoria");

            modelBuilder.Entity<Solicitud>()
                .Property(s => s.fecha_creacion)
                .HasDefaultValueSql("NOW()");


            modelBuilder.Entity<SolicitudTransferenciaDetalle>()
                .HasOne(d => d.Solicitud)
                .WithMany(s => s.transferencia_detalles) 
                .HasForeignKey(d => d.solicitud_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SolicitudTransferenciaDetalle>()
                .HasOne(d => d.Bien)
                .WithMany()
                .HasForeignKey(d => d.bien_id);

            modelBuilder.Entity<SolicitudTransferenciaDetalle>()
                .HasOne(d => d.AreaOrigen)
                .WithMany()
                .HasForeignKey(d => d.area_origen_id);

            modelBuilder.Entity<SolicitudTransferenciaDetalle>()
                .HasOne(d => d.AreaDestino)
                .WithMany()
                .HasForeignKey(d => d.area_destino_id);
        }
    }
}