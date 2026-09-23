using Microsoft.AspNetCore.Mvc;

namespace AsadaSJM.Controllers;

public class TestController : Controller
{
    public IActionResult Hash()
    {
        string contraseña = "123456";

        string hash = BCrypt.Net.BCrypt.HashPassword(contraseña);

        return Content(hash);
    }
}
