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
    public DbSet<Mision> Misiones => Set<Mision>();
    public DbSet<ProgresoMision> ProgresosMision => Set<ProgresoMision>();
    public DbSet<Insignia> Insignias => Set<Insignia>();
    public DbSet<UsuarioInsignia> UsuariosInsignias => Set<UsuarioInsignia>();
    public DbSet<ProgresoEducativo> ProgresosEducativos => Set<ProgresoEducativo>();
    public DbSet<MetaComunitaria> MetasComunitarias => Set<MetaComunitaria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<RegistroReciclaje>().ToTable("RegistrosReciclaje");
        modelBuilder.Entity<Recompensa>().ToTable("Recompensas");
        modelBuilder.Entity<Canje>().ToTable("Canjes");
        modelBuilder.Entity<SolicitudRetiro>().ToTable("SolicitudesRetiro");
        modelBuilder.Entity<PuntoLimpio>().ToTable("PuntosLimpios");
        modelBuilder.Entity<ContenidoEducativo>().ToTable("ContenidosEducativos");
        modelBuilder.Entity<Mision>().ToTable("Misiones");
        modelBuilder.Entity<ProgresoMision>().ToTable("ProgresosMision");
        modelBuilder.Entity<Insignia>().ToTable("Insignias");
        modelBuilder.Entity<UsuarioInsignia>().ToTable("UsuariosInsignias");
        modelBuilder.Entity<ProgresoEducativo>().ToTable("ProgresosEducativos");
        modelBuilder.Entity<MetaComunitaria>().ToTable("MetasComunitarias");

        modelBuilder.Entity<Usuario>().HasIndex(x => x.Correo).IsUnique();
        modelBuilder.Entity<Canje>().HasIndex(x => x.Codigo).IsUnique();
        modelBuilder.Entity<Insignia>().HasIndex(x => x.Codigo).IsUnique();
        modelBuilder.Entity<RegistroReciclaje>().Property(x => x.PesoKg).HasPrecision(8, 2);
        modelBuilder.Entity<Mision>().Property(x => x.Objetivo).HasPrecision(10, 2);
        modelBuilder.Entity<ProgresoMision>().Property(x => x.Progreso).HasPrecision(10, 2);
        modelBuilder.Entity<MetaComunitaria>().Property(x => x.ObjetivoKg).HasPrecision(10, 2);
        modelBuilder.Entity<ProgresoMision>().HasKey(x => new { x.UsuarioId, x.MisionId });
        modelBuilder.Entity<UsuarioInsignia>().HasKey(x => new { x.UsuarioId, x.InsigniaId });
        modelBuilder.Entity<ProgresoEducativo>().HasKey(x => new { x.UsuarioId, x.ContenidoEducativoId });
        modelBuilder.Entity<Usuario>().HasMany(x => x.RegistrosReciclaje).WithOne(x => x.Usuario)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Canje>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Canje>().HasOne(x => x.Recompensa).WithMany()
            .HasForeignKey(x => x.RecompensaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SolicitudRetiro>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RegistroReciclaje>().HasOne(x => x.PuntoLimpio).WithMany()
            .HasForeignKey(x => x.PuntoLimpioId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ProgresoMision>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProgresoMision>().HasOne(x => x.Mision).WithMany()
            .HasForeignKey(x => x.MisionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioInsignia>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioInsignia>().HasOne(x => x.Insignia).WithMany()
            .HasForeignKey(x => x.InsigniaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProgresoEducativo>().HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProgresoEducativo>().HasOne(x => x.ContenidoEducativo).WithMany()
            .HasForeignKey(x => x.ContenidoEducativoId).OnDelete(DeleteBehavior.Cascade);
    }
}
