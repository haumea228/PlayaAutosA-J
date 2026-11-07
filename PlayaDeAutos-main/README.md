# PlayaAutos — Sistema de Gestión de Concesionario

Sistema web para la administración de un concesionario de autos usados. Permite gestionar el inventario de vehículos, clientes, ventas, citas y facturación.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Backend API | ASP.NET Core 10 Web API |
| Frontend Web | ASP.NET Core 10 MVC |
| Base de datos | SQL Server 2019 / Express |
| ORM | Entity Framework Core 10 (Code First) |
| Autenticación | JWT Bearer |
| UI | Bootstrap 5.3 + Font Awesome 6 + Google Fonts Inter |

## Estructura del proyecto

```
PlayaAutos/
├── PlayaAutos.API/          # Web API REST (puerto 7001)
│   ├── Controllers/         # Endpoints REST
│   ├── Data/                # DbContext (AppDbContext)
│   ├── DTOs/                # Objetos de transferencia
│   ├── Helpers/             # JwtHelper, password hashing
│   ├── Migrations/          # Migraciones EF Core
│   └── Models/              # Entidades de dominio
│
├── PlayaAutos.Web/          # Aplicación MVC (puerto 7056)
│   ├── Controllers/         # Controladores MVC
│   ├── Models/              # ViewModels
│   ├── Services/            # ApiService (cliente HTTP)
│   ├── Views/               # Vistas Razor
│   └── wwwroot/             # Archivos estáticos (CSS, uploads)
│
└── PlayaAutos.slnx          # Solución Visual Studio
```

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019 o SQL Server Express
- Visual Studio 2022+ o VS Code con extensión C#

## Configuración inicial

### 1. Base de datos

Editar la cadena de conexión en `PlayaAutos.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=PlayaAutosDB;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### 2. Crear la base de datos

```bash
cd PlayaAutos.API
dotnet ef database update
```

### 3. Configurar la URL de la API en el Web

Editar `PlayaAutos.Web/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/"
  }
}
```

### 4. Ejecutar

Iniciar ambos proyectos simultáneamente (en Visual Studio: clic derecho en la solución → *Set Startup Projects* → seleccionar ambos).

O desde consola en terminales separadas:

```bash
# Terminal 1
cd PlayaAutos.API
dotnet run

# Terminal 2
cd PlayaAutos.Web
dotnet run
```

### 5. Credenciales por defecto

El sistema crea un usuario administrador al iniciar por primera vez si la tabla está vacía:

| Campo | Valor |
|---|---|
| Email | `admin@playaautos.com` |
| Contraseña | `Admin123!` |
| Rol | `AdministradorP` |

## Módulos implementados

- **Autenticación** — Login con JWT, sesión en servidor
- **Vehículos** — Listado, registro, edición, eliminación, carga de imágenes
- **Dashboard** — Resumen de estadísticas del concesionario

## Módulos pendientes

- Clientes
- Ventas
- Citas
- Facturación
- Caja

## Carga inicial de datos de catálogo

Los catálogos (marcas, modelos, tipos de vehículo, condiciones, estados, orígenes) se insertan manualmente en SQL Server. Scripts disponibles en la documentación interna del proyecto.

## Imágenes de vehículos

Las fotos se guardan en `PlayaAutos.Web/wwwroot/uploads/vehiculos/`. Esta carpeta no está incluida en el repositorio. Asegurarse de crearla antes de registrar vehículos, o dejar que el sistema la cree automáticamente al subir la primera imagen.

Agregar al `.gitignore`:

```
wwwroot/uploads/
```

## Variables de entorno recomendadas para producción

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "JwtSettings": {
    "Key": "clave-secreta-larga-minimo-32-caracteres",
    "Issuer": "PlayaAutosAPI",
    "Audience": "PlayaAutosWeb",
    "ExpireHours": 8
  }
}
```

## Licencia

Uso interno — A&J Autos Usados.
