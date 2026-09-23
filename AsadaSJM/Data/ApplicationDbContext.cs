using AsadaSJM.Models;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Abonado> Abonados => Set<Abonado>();
    public DbSet<Averia> Averias => Set<Averia>();
    public DbSet<Tramite> Tramites => Set<Tramite>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Recibo> Recibos => Set<Recibo>();
    public DbSet<Bitacora> Bitacoras => Set<Bitacora>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =====================================================
        // TABLAS REALES DE ASADA_SJM
        // =====================================================

        modelBuilder.Entity<Rol>()
            .ToTable("AspNetRoles");

        modelBuilder.Entity<Usuario>()
            .ToTable("USUARIO");

        modelBuilder.Entity<Abonado>()
            .ToTable("ABONADO");

        modelBuilder.Entity<Averia>()
            .ToTable("AVERIA");

        modelBuilder.Entity<Tramite>()
            .ToTable("TRAMITE");

        modelBuilder.Entity<Documento>()
            .ToTable("TRAMITE_DOCUMENTO");

        modelBuilder.Entity<Recibo>()
            .ToTable("RECIBO");

        modelBuilder.Entity<Bitacora>()
            .ToTable("BITACORA");


        // =====================================================
        // RELACIÓN ABONADO -> AVERIA
        // =====================================================

        modelBuilder.Entity<Averia>()
            .HasOne(a => a.Abonado)
            .WithMany(a => a.Averias)
            .HasForeignKey(a => a.IdAbonado)
            .OnDelete(DeleteBehavior.Restrict);


        // =====================================================
        // RELACIÓN USUARIO -> AVERIA
        // =====================================================

        modelBuilder.Entity<Averia>()
            .HasOne(a => a.UsuarioReporta)
            .WithMany()
            .HasForeignKey(a => a.IdUsuarioReporta)
            .OnDelete(DeleteBehavior.Restrict);


        // =====================================================
        // RELACIONES DE TRAMITE
        // =====================================================

        modelBuilder.Entity<Tramite>()
            .HasOne(t => t.Abonado)
            .WithMany(a => a.Tramites)
            .HasForeignKey(t => t.IdAbonado)
            .OnDelete(DeleteBehavior.Restrict);


        // =====================================================
        // RELACIÓN RECIBO -> ABONADO
        // =====================================================

        modelBuilder.Entity<Recibo>()
            .HasOne(r => r.Abonado)
            .WithMany(a => a.Recibos)
            .HasForeignKey(r => r.IdAbonado)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
