using Microsoft.EntityFrameworkCore;
using Reciclame.Api.Models;

namespace Reciclame.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RegistroReciclaje> RegistrosReciclaje => Set<RegistroReciclaje>();
    public DbSet<Recompensa> Recompensas => Set<Recompensa>();
    public DbSet<Canje> Canjes => Set<Canje>();
    public DbSet<SolicitudRetiro> SolicitudesRetiro => Set<SolicitudRetiro>();
    public DbSet<PuntoLimpio> PuntosLimpios => Set<PuntoLimpio>();
    public DbSet<ContenidoEducativo> ContenidosEducativos => Set<ContenidoEducativo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<RegistroReciclaje>().ToTable("RegistrosReciclaje");
        modelBuilder.Entity<Recompensa>().ToTable("Recompensas");
        modelBuilder.Entity<Canje>().ToTable("Canjes");
        modelBuilder.Entity<SolicitudRetiro>().ToTable("SolicitudesRetiro");
        modelBuilder.Entity<PuntoLimpio>().ToTable("PuntosLimpios");
        modelBuilder.Entity<ContenidoEducativo>().ToTable("ContenidosEducativos");

        modelBuilder.Entity<Usuario>().HasIndex(x => x.Correo).IsUnique();
        modelBuilder.Entity<Canje>().HasIndex(x => x.Codigo).IsUnique();
        modelBuilder.Entity<RegistroReciclaje>().Property(x => x.PesoKg).HasPrecision(8, 2);
        modelBuilder.Entity<Usuario>().HasMany(x => x.RegistrosReciclaje).WithOne(x => x.Usuario)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Canje>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Canje>().HasOne(x => x.Recompensa).WithMany()
            .HasForeignKey(x => x.RecompensaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SolicitudRetiro>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
