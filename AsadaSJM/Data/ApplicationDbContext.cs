
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
    public DbSet<BitacoraItemViewModel> Bitacoras => Set<BitacoraItemViewModel>();
    public DbSet<Expresidente> Expresidentes => Set<Expresidente>();
    public DbSet<InformacionInstitucional> InformacionInstitucional => Set<InformacionInstitucional>();

    // =====================================================
    // MGA-01: GESTIÓN AMBIENTAL
    // =====================================================

    public DbSet<Naciente> Nacientes => Set<Naciente>();
    public DbSet<Tanque> Tanques => Set<Tanque>();

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

        modelBuilder.Entity<BitacoraItemViewModel>()
            .ToTable("BITACORA");

        modelBuilder.Entity<Expresidente>()
            .ToTable("EXPRESIDENTE");

        modelBuilder.Entity<InformacionInstitucional>()
            .ToTable("INFORMACION_INSTITUCIONAL");

        // =====================================================
        // MGA-01: TABLA NACIENTE
        // =====================================================

        modelBuilder.Entity<Naciente>(entity =>
        {
            entity.ToTable("NACIENTE");

            entity.HasKey(n => n.IdNaciente);

            entity.Property(n => n.IdNaciente)
                .ValueGeneratedOnAdd();

            entity.Property(n => n.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(n => n.Ubicacion)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(n => n.Descripcion)
                .IsRequired();

            entity.Property(n => n.Estado)
                .HasDefaultValue(true);

            entity.Property(n => n.FechaRegistro)
                .HasDefaultValueSql("GETDATE()");
        });

        // =====================================================
        // MGA-01: TABLA TANQUE
        // =====================================================

        modelBuilder.Entity<Tanque>(entity =>
        {
            entity.ToTable("TANQUE");

            entity.HasKey(t => t.IdTanque);

            entity.Property(t => t.IdTanque)
                .ValueGeneratedOnAdd();

            entity.Property(t => t.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(t => t.Ubicacion)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(t => t.Descripcion)
                .IsRequired();

            entity.Property(t => t.Estado)
                .HasDefaultValue(true);

            entity.Property(t => t.FechaRegistro)
                .HasDefaultValueSql("GETDATE()");
        });

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
