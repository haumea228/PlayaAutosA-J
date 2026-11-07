using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlayaAutos.Web.Controllers
{
    /// <summary>
    /// Controlador base para vistas exclusivas de Administrador y Vendedor.
    /// Redirige a Login si el usuario no tiene rol autorizado.
    /// </summary>
    public class AdminVendedorController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "AdministradorP" && rol != "Vendedor" && rol != "Cajero")
            {
                context.Result = RedirectToAction("Login", "Auth");
            }
            base.OnActionExecuting(context);
        }
    }
}