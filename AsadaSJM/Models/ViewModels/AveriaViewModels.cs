using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace AsadaSJM.Models;

public class CategoriaAveriaItem
{
    public int IdCategoriaAveria { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ReportarAveriaViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int IdCategoriaAveria { get; set; }

    [Required(ErrorMessage = "Describa la avería.")]
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique la ubicación de la avería.")]
    [StringLength(500, ErrorMessage = "La ubicación no puede superar los 500 caracteres.")]
    public string Ubicacion { get; set; } = string.Empty;

    public List<IFormFile> Fotografias { get; set; } = new();

    public List<CategoriaAveriaItem> Categorias { get; set; } = new();
}

public class AveriaSeguimientoItem
{
    public DateTime Fecha { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
}

public class AveriaViewModel
{
    public int IdAveria { get; set; }
    public string NumeroCaso { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaReporte { get; set; }
    public DateTime? FechaAtencion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public string NIS { get; set; } = string.Empty;
    public string Abonado { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    public List<string> Imagenes { get; set; } = new();
    public List<AveriaSeguimientoItem> Seguimiento { get; set; } = new();
}

public class MisAveriasViewModel
{
    public string? NumeroCasoBuscado { get; set; }
    public List<AveriaViewModel> Averias { get; set; } = new();
}

public class AveriasAdminViewModel
{
    public string? EstadoFiltro { get; set; }
    public List<AveriaViewModel> Averias { get; set; } = new();
}

public class ActualizarEstadoAveriaViewModel
{
    public int IdAveria { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaAtencion { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}

// Lee los resultados de los procedimientos SP_Averia_*
public static class AveriaLector
{
    private static string Texto(SqlDataReader r, string columna) =>
        r[columna] == DBNull.Value ? string.Empty : r[columna].ToString() ?? string.Empty;

    private static DateTime? Fecha(SqlDataReader r, string columna) =>
        r[columna] == DBNull.Value ? null : Convert.ToDateTime(r[columna]);

    public static AveriaViewModel Leer(SqlDataReader r) => new()
    {
        IdAveria = Convert.ToInt32(r["IdAveria"]),
        NumeroCaso = Texto(r, "NumeroCaso"),
        Categoria = Texto(r, "Categoria"),
        Descripcion = Texto(r, "Descripcion"),
        Ubicacion = Texto(r, "Ubicacion"),
        Prioridad = Texto(r, "Prioridad"),
        Estado = Texto(r, "Estado"),
        FechaReporte = Convert.ToDateTime(r["FechaReporte"]),
        FechaAtencion = Fecha(r, "FechaAtencion"),
        FechaResolucion = Fecha(r, "FechaResolucion"),
        Observaciones = Texto(r, "Observaciones"),
        NIS = Texto(r, "NIS"),
        Abonado = Texto(r, "Abonado"),
        Telefono = Texto(r, "Telefono")
    };

    // Para SP_Averia_DetalleAbonado y SP_Averia_DetalleAdmin (3 resultados)
    public static AveriaViewModel? LeerDetalle(SqlDataReader r)
    {
        if (!r.Read())
        {
            return null;
        }

        AveriaViewModel modelo = Leer(r);

        r.NextResult();
        while (r.Read())
        {
            modelo.Imagenes.Add(Texto(r, "RutaImagen"));
        }

        r.NextResult();
        while (r.Read())
        {
            modelo.Seguimiento.Add(new AveriaSeguimientoItem
            {
                Fecha = Convert.ToDateTime(r["Fecha"]),
                EstadoAnterior = Texto(r, "EstadoAnterior"),
                EstadoNuevo = Texto(r, "EstadoNuevo"),
                Comentario = Texto(r, "Comentario"),
                Usuario = Texto(r, "Usuario")
            });
        }

        return modelo;
    }
}
