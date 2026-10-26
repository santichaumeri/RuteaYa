using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;

namespace RuteaYa.Logica.Data;

public class RuteaYaDbContext : DbContext
{
    public RuteaYaDbContext(DbContextOptions<RuteaYaDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Viaje> Viajes => Set<Viaje>();
    public DbSet<Parada> Paradas => Set<Parada>();
    public DbSet<Envio> Envios => Set<Envio>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            e.Property(x => x.Apellido).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Viaje>(e =>
        {
            e.Property(x => x.Origen).IsRequired().HasMaxLength(300);
            e.Property(x => x.Destino).IsRequired().HasMaxLength(300);
            e.Property(x => x.PesoMaximoKg).HasPrecision(18, 2);
            e.Property(x => x.DesvioMaximoKm).HasPrecision(18, 2);

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Viajes)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Parada>(e =>
        {
            e.Property(x => x.Direccion).IsRequired().HasMaxLength(300);
            e.HasIndex(x => new { x.ViajeId, x.Orden }).IsUnique();

            e.HasOne(x => x.Viaje)
                .WithMany(v => v.Paradas)
                .HasForeignKey(x => x.ViajeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Envio>(e =>
        {
            e.Property(x => x.Origen).IsRequired().HasMaxLength(300);
            e.Property(x => x.Destino).IsRequired().HasMaxLength(300);
            e.Property(x => x.Descripcion).IsRequired().HasMaxLength(500);
            e.Property(x => x.PesoKg).HasPrecision(18, 2);
            e.Property(x => x.MontoOfrecido).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.Envios)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asignacion>(e =>
        {
            e.Property(x => x.DesvioKm).HasPrecision(18, 2);
            e.Property(x => x.Ganancia).HasPrecision(18, 2);

            // Un envío solo puede asignarse una vez.
            e.HasIndex(x => x.EnvioId).IsUnique();

            e.HasOne(x => x.Viaje)
                .WithMany(v => v.Asignaciones)
                .HasForeignKey(x => x.ViajeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Envio)
                .WithOne(v => v.Asignacion)
                .HasForeignKey<Asignacion>(x => x.EnvioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
