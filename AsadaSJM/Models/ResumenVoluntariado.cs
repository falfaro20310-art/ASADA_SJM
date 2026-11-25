namespace AsadaSJM.Models;

// MGA-04: resumen de personas inscritas por campaña.
// No es una tabla: se calcula con un GROUP BY sobre las inscripciones.

public class ResumenVoluntariado
{
    public int? IdCampana { get; set; }

    public int Total { get; set; }

    public DateTime UltimaInscripcion { get; set; }
}