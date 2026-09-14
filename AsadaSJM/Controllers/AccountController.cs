using Microsoft.AspNetCore.Mvc;

namespace AsadaSJM.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string correo, string contrasena)
    {
        // TODO: reemplazar por validación real contra ASP.NET Core Identity / Usuarios
        if (!string.IsNullOrWhiteSpace(correo) && !string.IsNullOrWhiteSpace(contrasena))
        {
            return RedirectToAction("Index", "Home");
        }
        ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
        return View();
    }
}
