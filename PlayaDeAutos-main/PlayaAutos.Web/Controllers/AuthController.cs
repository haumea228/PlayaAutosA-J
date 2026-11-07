using Microsoft.AspNetCore.Mvc;
using PlayaAutos.Web.Models;
using PlayaAutos.Web.Services;

namespace PlayaAutos.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var token = HttpContext.Session.GetString("Token");
            if (token != null)
            {
                var rol = HttpContext.Session.GetString("Rol");
                return rol == "Cliente"
                    ? RedirectToAction("IndexCliente", "Catalogo")
                    : RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await _apiService.PostAsync("api/Auth/login", new
            {
                email = model.Email,
                password = model.Password
            });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos.");
                return View(model);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result == null)
            {
                ModelState.AddModelError("", "Error al procesar la respuesta.");
                return View(model);
            }

            HttpContext.Session.SetString("Token", result.Token);
            HttpContext.Session.SetString("Nombre", result.Nombre);
            HttpContext.Session.SetString("Rol", result.Rol);
            HttpContext.Session.SetInt32("UsuarioId", result.UsuarioId);

            if (result.Rol == "Cliente")
                return RedirectToAction("IndexCliente", "Catalogo");
            else if (result.Rol == "Cajero")
                return RedirectToAction("Pendientes", "Ventas");
            else
                return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Registro()
        {
            if (HttpContext.Session.GetString("Token") != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(RegistroClienteViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await _apiService.PostAsync("api/Auth/registro-cliente", new
            {
                nombre = model.Nombre,
                email = model.Email,
                password = model.Password,
                telefono = model.Telefono
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error.Trim('"'));
                return View(model);
            }

            TempData["RegistroExitoso"] = "¡Cuenta creada exitosamente! Ya podés iniciar sesión.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Catalogo");
        }
    }
}
