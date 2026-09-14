using AsadaSJM.Models;
using Microsoft.EntityFrameworkCore;

namespace AsadaSJM.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

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

        // Evitar múltiples cascadas conflictivas en Averia/Tramite -> Usuario
        modelBuilder.Entity<Averia>()
            .HasOne(a => a.Responsable)
            .WithMany()
            .HasForeignKey(a => a.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tramite>()
            .HasOne(t => t.Gestor)
            .WithMany()
            .HasForeignKey(t => t.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull);

        // ---------- Datos semilla ----------
        modelBuilder.Entity<Rol>().HasData(
            new Rol { IdRol = 1, NombreRol = "Administrador" },
            new Rol { IdRol = 2, NombreRol = "Operativo" },
            new Rol { IdRol = 3, NombreRol = "Abonado" }
        );

        modelBuilder.Entity<Usuario>().HasData(
            new Usuario { IdUsuario = 1, Nombre = "P. Jiménez", Correo = "pjimenez@asadasjm.cr", ContrasenaHash = "temporal", IdRol = 1 },
            new Usuario { IdUsuario = 2, Nombre = "C. Mora", Correo = "cmora@asadasjm.cr", ContrasenaHash = "temporal", IdRol = 2 }
        );

        modelBuilder.Entity<Abonado>().HasData(
            new Abonado { IdAbonado = 1, Nombre = "María Vargas", Direccion = "Calle Los Robles", NumeroMedidor = "MED-0231" },
            new Abonado { IdAbonado = 2, Nombre = "Juan Rojas", Direccion = "Barrio Central", NumeroMedidor = "MED-0198" },
            new Abonado { IdAbonado = 3, Nombre = "Lucía Solano", Direccion = "Calle La Montaña", NumeroMedidor = "MED-0177", Activo = false }
        );

        modelBuilder.Entity<Averia>().HasData(
            new Averia
            {
                IdAveria = 1,
                IdAbonado = 1,
                Descripcion = "Fuga en tubería principal",
                Estado = "Pendiente",
                FechaReporte = new DateTime(2026, 9, 1, 8, 0, 0)
            },
            new Averia
            {
                IdAveria = 2,
                IdAbonado = 2,
                IdUsuario = 2,
                Descripcion = "Baja presión de agua",
                Estado = "En proceso",
                FechaReporte = new DateTime(2026, 9, 2, 9, 30, 0)
            },
            new Averia
            {
                IdAveria = 3,
                IdAbonado = 3,
                IdUsuario = 2,
                Descripcion = "Medidor dañado",
                Estado = "Resuelto",
                FechaReporte = new DateTime(2026, 9, 3, 10, 0, 0)
            }
        );
        modelBuilder.Entity<Tramite>().HasData(
            new Tramite
            {
                IdTramite = 1,
                IdAbonado = 1,
                TipoTramite = "Cambio de titular",
                Estado = "Pendiente",
                FechaSolicitud = new DateTime(2026, 9, 1, 8, 30, 0)
            },
            new Tramite
            {
                IdTramite = 2,
                IdAbonado = 2,
                TipoTramite = "Solicitud de conexión",
                Estado = "En revisión",
                FechaSolicitud = new DateTime(2026, 9, 2, 10, 0, 0)
            },
            new Tramite
            {
                IdTramite = 3,
                IdAbonado = 3,
                TipoTramite = "Reclamo de facturación",
                Estado = "Aprobado",
                FechaSolicitud = new DateTime(2026, 9, 3, 11, 0, 0)
            }
        );
        modelBuilder.Entity<Recibo>().HasData(
            new Recibo
            {
                IdRecibo = 1,
                IdAbonado = 1,
                Monto = 6450,
                FechaEmision = new DateTime(2026, 9, 1)
            },
            new Recibo
            {
                IdRecibo = 2,
                IdAbonado = 2,
                Monto = 5120,
                FechaEmision = new DateTime(2026, 9, 1)
            }
        );
    }
}
