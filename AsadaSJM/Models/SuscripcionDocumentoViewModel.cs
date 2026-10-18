using System.ComponentModel.DataAnnotations;

namespace AsadaSJM.Models;

public class SuscripcionDocumentoViewModel
{
    public List<CategoriaDocumento> Categorias { get; set; } =
        new();

    [Display(Name = "Categorías seleccionadas")]
    public List<int> CategoriasSeleccionadas { get; set; } =
        new();
}
