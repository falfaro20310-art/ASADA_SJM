using Microsoft.AspNetCore.Mvc;

namespace AsadaSJM.Controllers;

// Sitio público (institucional) de la ASADA — separado del panel administrativo
public class PortalController : Controller
{
    public IActionResult Index() => View();
}
