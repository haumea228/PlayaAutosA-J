using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlayaAutos.API.Data;
using PlayaAutos.API.Helpers;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<PlayaAutos.API.Services.CuotaPagoService>();
builder.Services.AddSingleton<PlayaAutos.API.Services.ComprobanteStorage>();
builder.Services.AddSingleton<PlayaAutos.API.Services.FirebasePushSender>();
builder.Services.AddScoped<PlayaAutos.API.Services.NotificacionesService>();

// Emails (Gmail de la empresa por SMTP) y recuperación de contraseña con código
builder.Services.Configure<PlayaAutos.API.Services.CorreoOptions>(builder.Configuration.GetSection("Correo"));
builder.Services.AddSingleton<PlayaAutos.API.Services.CorreoService>();
builder.Services.AddScoped<PlayaAutos.API.Services.RecuperacionContrasenaService>();

// Recordatorios push de cuotas próximas a vencer (revisión periódica en segundo plano)
builder.Services.Configure<PlayaAutos.API.Services.RecordatoriosOptions>(builder.Configuration.GetSection("Recordatorios"));
builder.Services.AddScoped<PlayaAutos.API.Services.RecordatoriosCuotaService>();
builder.Services.AddHostedService<PlayaAutos.API.Services.RecordatoriosCuotaWorker>();

// Pago de cuotas con tarjeta (Stripe Checkout, entorno de pruebas)
builder.Services.Configure<PlayaAutos.API.Services.PagosOptions>(builder.Configuration.GetSection("Pagos"));
builder.Services.AddSingleton<PlayaAutos.API.Services.StripeGateway>();
builder.Services.AddScoped<PlayaAutos.API.Services.PagoOnlineService>();

// Asistente de recomendaciones (chat compatible con OpenAI: Groq / Gemini / Ollama)
builder.Services.Configure<PlayaAutos.API.Services.AsistenteOptions>(builder.Configuration.GetSection("Asistente"));
builder.Services.AddHttpClient<PlayaAutos.API.Services.AsistenteService>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
        await ctx.HttpContext.Response.WriteAsync("Hiciste muchas consultas seguidas. Esperá un minuto y volvé a intentar.", ct);
    // Por IP: protege el plan gratuito del proveedor de IA.
    o.AddPolicy(PlayaAutos.API.Controllers.AsistenteController.PoliticaLimite, ctx =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Recuperación de contraseña: frena el envío masivo de emails y que se
    // prueben códigos al azar (además del límite de intentos por código).
    o.AddPolicy(PlayaAutos.API.Controllers.AuthController.PoliticaRecuperacion, ctx =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

// --- Seed usuario admin por defecto ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Usuarios.Any())
    {
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("Admin123!")
            )
        ).ToLower();

        db.Usuarios.Add(new PlayaAutos.API.Models.Usuario
        {
            UsuarioNombre    = "Administrador",
            UsuarioEmail     = "admin@playaautos.com",
            PasswordHash     = hash,
            Rol              = "AdministradorP",
            ActivoUsuario    = true,
            FechaAltaUsuario = DateTime.Now
        });
        db.SaveChanges();
    }
}

app.Run();