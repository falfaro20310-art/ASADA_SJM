
using AsadaSJM.Data.Entidades;
using AsadaSJM.Models;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
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

    public DbSet<BitacoraItemViewModel> Bitacoras
        => Set<BitacoraItemViewModel>();

    public DbSet<Expresidente> Expresidentes
        => Set<Expresidente>();

    public DbSet<InformacionInstitucional> InformacionInstitucional
        => Set<InformacionInstitucional>();

    public DbSet<GaleriaEntidad> Galerias
        => Set<GaleriaEntidad>();

    public DbSet<FotoGaleriaEntidad> FotosGaleria
        => Set<FotoGaleriaEntidad>();

    // =====================================================
    // MGA-01: GESTIÓN AMBIENTAL EXISTENTE
    // =====================================================

    public DbSet<Naciente> Nacientes
        => Set<Naciente>();

    public DbSet<Tanque> Tanques
        => Set<Tanque>();

    // =====================================================
    // MGA-03, MGA-04 Y MGA-05: NUEVAS TABLAS
    // =====================================================

    public DbSet<CampanaAmbiental> CampanasAmbientales
        => Set<CampanaAmbiental>();

    public DbSet<InscripcionVoluntariado> InscripcionesVoluntariado
        => Set<InscripcionVoluntariado>();

    public DbSet<FotoProyectoAmbiental> FotosProyectosAmbientales
        => Set<FotoProyectoAmbiental>();

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

        modelBuilder.Entity<GaleriaEntidad>()
            .ToTable("GALERIA");

        modelBuilder.Entity<FotoGaleriaEntidad>()
            .ToTable("FOTO_GALERIA");

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
        // MGA-03: CAMPAÑAS AMBIENTALES
        // =====================================================

        modelBuilder.Entity<CampanaAmbiental>(entity =>
        {
            entity.ToTable("CAMPANA_AMBIENTAL");

            entity.HasKey(c => c.IdCampana);

            entity.Property(c => c.IdCampana)
                .ValueGeneratedOnAdd();

            entity.Property(c => c.Titulo)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(c => c.Descripcion)
                .IsRequired();

            entity.Property(c => c.Lugar)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(c => c.Fecha)
                .HasColumnType("date");

            entity.Property(c => c.ImagenUrl)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(c => c.FechaRegistro)
                .HasDefaultValueSql("GETDATE()");
        });

        // =====================================================
        // MGA-04: INSCRIPCIONES DE VOLUNTARIADO
        // =====================================================

        modelBuilder.Entity<InscripcionVoluntariado>(entity =>
        {
            entity.ToTable("INSCRIPCION_VOLUNTARIADO");

            entity.HasKey(i => i.IdInscripcion);

            entity.Property(i => i.IdInscripcion)
                .ValueGeneratedOnAdd();

            entity.Property(i => i.Nombre)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(i => i.Correo)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(i => i.Telefono)
                .HasMaxLength(25)
                .IsRequired();

            entity.Property(i => i.FechaRegistro)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne<CampanaAmbiental>()
                .WithMany()
                .HasForeignKey(i => i.IdCampana)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // =====================================================
        // MGA-05: FOTOGRAFÍAS AMBIENTALES
        //
        // Independiente de GALERIA y FOTO_GALERIA.
        // =====================================================

        modelBuilder.Entity<FotoProyectoAmbiental>(entity =>
        {
            entity.ToTable("FOTO_PROYECTO_AMBIENTAL");

            entity.HasKey(f => f.IdFoto);

            entity.Property(f => f.IdFoto)
                .ValueGeneratedOnAdd();

            entity.Property(f => f.Titulo)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(f => f.ImagenUrl)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(f => f.FechaRegistro)
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

        // =====================================================
        // RELACIÓN USUARIO -> GALERIA
        // =====================================================

        modelBuilder.Entity<GaleriaEntidad>()
            .HasOne(g => g.Usuario)
            .WithMany()
            .HasForeignKey(g => g.IdUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        // =====================================================
        // RELACIÓN GALERIA -> FOTO_GALERIA
        // =====================================================

        modelBuilder.Entity<FotoGaleriaEntidad>()
            .HasOne(f => f.Galeria)
            .WithMany(g => g.Fotos)
            .HasForeignKey(f => f.IdGaleria)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
