# Sistema Web ASADA SJM — Proyecto ASP.NET Core MVC + EF Core

## Requisitos
- Visual Studio 2022 Community (con la carga de trabajo "Desarrollo web y ASP.NET")
- .NET 9 SDK (Visual Studio lo puede instalar desde el instalador)
- SQL Server LocalDB (incluido con Visual Studio) o SQL Server Express

## Cómo abrirlo
1. Descomprima el .zip.
2. Abra `AsadaSJM.sln` con Visual Studio Community.
3. Espere a que restaure los paquetes NuGet automáticamente
   (Microsoft.EntityFrameworkCore.SqlServer, .Tools, .Design).

## Crear la base de datos
Abra la **Consola del Administrador de paquetes** (Tools > NuGet Package Manager >
Package Manager Console) y ejecute, dentro de la carpeta del proyecto AsadaSJM:

```
Add-Migration InicialAsadaSJM
Update-Database
```

Esto creará la base `AsadaSJMDb` en LocalDB con las tablas (Roles, Usuarios,
Abonados, Averias, Tramites, Documentos, Recibos, Bitacoras) y los datos semilla
definidos en `Data/ApplicationDbContext.cs`.

Si prefiere usar SQL Server en vez de LocalDB, cambie la cadena de conexión en
`appsettings.json`.

## Ejecutar
Presione F5 (o el botón ▶ en Visual Studio). El sistema abre en la vista de
**Inicio** (más adelante puede enrutar primero a `/Account/Login` como página de
entrada si desea forzar el login).

## Estructura (arquitectura por capas / MVC)
- `Models/` — Entidades (capa de datos / dominio): Rol, Usuario, Abonado, Averia,
  Tramite, Documento, Recibo, Bitacora — reflejan el diagrama entidad-relación del
  documento de arquitectura.
- `Data/ApplicationDbContext.cs` — Capa de acceso a datos (Entity Framework Core).
- `Controllers/` — Capa de aplicación (controladores MVC).
- `Views/` — Capa de presentación (Razor Views), con `Views/Shared/_Layout.cshtml`
  como plantilla común (sidebar con los módulos) y `wwwroot/css/site.css` con la
  paleta y tipografías definidas en el documento (Azul Agua #004A99, Verde Bosque
  #2D5A27, Montserrat/Roboto).

## Notas
- El módulo de **Abonados** tiene CRUD completo (Index, Create, Edit, Delete-lógico)
  como ejemplo a replicar en los demás módulos.
- Los módulos de Averías, Trámites, Documentos, Recibos y Usuarios están en modo
  lectura (Index) con datos semilla; puede clonar el patrón de AbonadosController
  para agregarles Create/Edit.
- El login (`AccountController`) es un esqueleto: valida que los campos no estén
  vacíos, pero no verifica contra la tabla Usuarios todavía. Para producción se
  recomienda migrar a ASP.NET Core Identity, tal como indica el documento de
  arquitectura (sección 4.3).
