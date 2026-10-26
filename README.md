# RuteaYa

Plataforma de transporte colaborativo. ASP.NET Core MVC + Web API + Entity Framework Core + SQL Server (.NET 10).

## Estructura de la solución

| Proyecto | Qué contiene | Referencia a |
|---|---|---|
| `RuteaYa.Entidades` | Entidades y enums | nadie |
| `RuteaYa.Logica` | `DbContext`, migraciones, servicios, DTOs | `Entidades` |
| `RuteaYa.Api` | Controllers de la Web API | `Logica` |
| `RuteaYa.Web` | MVC (consume la API por HTTP) | nadie |
| `RuteaYa.Tests` | Tests unitarios (xUnit) | `Entidades`, `Logica`, `Api` |

Regla: `RuteaYa.Web` **no** referencia a otros proyectos ni usa EF/DbContext. Toda la información la pide a la API.

## Requisitos

- Visual Studio 2022 (o superior) con la carga de trabajo "ASP.NET y desarrollo web"
- .NET SDK 10
- SQL Server (LocalDB viene con Visual Studio) o SQL Server Express
- Herramienta de EF Core (se instala una sola vez):

```
dotnet tool install --global dotnet-ef
```

## 1. Clonar y configurar la base de datos

```
git clone <URL-DEL-REPO>
cd RuteaYa
```

La connection string está en `RuteaYa.Api/appsettings.json`. Por defecto usa LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=RuteaYa;Trusted_Connection=True;TrustServerCertificate=True
```

Si usás otro SQL Server, **no cambies `appsettings.json`** (se commitea y pisa a los demás). Creá `RuteaYa.Api/appsettings.Development.json` (está en el `.gitignore`) con tu cadena:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=RuteaYa;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## 2. Crear la base de datos (todo el equipo)

Las migraciones **ya están en el repo**. Solo hay que aplicarlas. Desde la carpeta de la solución (donde está `RuteaYa.slnx`):

```
dotnet ef database update -p RuteaYa.Logica -s RuteaYa.Api
```

Esto crea la base `RuteaYa` con todas las tablas. Si la base no existe, EF la crea.

Desde Visual Studio también se puede: *Ver → Otras ventanas → Consola del Administrador de paquetes*, seleccionar como **proyecto predeterminado** `RuteaYa.Logica` y ejecutar:

```
Update-Database
```

(El proyecto de inicio de la solución tiene que ser `RuteaYa.Api`.)

## 3. Ejecutar

1. Clic derecho en la solución → *Configurar proyectos de inicio* → *Varios proyectos* → `RuteaYa.Api` y `RuteaYa.Web` en "Iniciar".
2. F5.

## Migraciones: reglas del equipo

**Solo UNA persona genera migraciones.** Si dos personas generan migraciones al mismo tiempo, el historial se rompe y hay que borrar y rehacer.

### Quien genera la migración inicial (una sola vez, una sola persona)

1. Verificar que la solución compile.
2. Desde la carpeta de la solución:

```
dotnet ef migrations add InitialCreate -p RuteaYa.Logica -s RuteaYa.Api
dotnet ef database update -p RuteaYa.Logica -s RuteaYa.Api
```

3. Se crea la carpeta `RuteaYa.Logica/Migrations/`. Revisar que existan los archivos `..._InitialCreate.cs`, `..._InitialCreate.Designer.cs` y `RuteaYaDbContextModelSnapshot.cs`.
4. **Probar en una base limpia**: borrar la base y volver a correr `database update`:

```
dotnet ef database drop -p RuteaYa.Logica -s RuteaYa.Api
dotnet ef database update -p RuteaYa.Logica -s RuteaYa.Api
```

5. Commitear **toda** la carpeta `Migrations/` (incluido el snapshot) y avisar al equipo.

### Todos los demás

```
git pull
dotnet ef database update -p RuteaYa.Logica -s RuteaYa.Api
```

No ejecutar nunca `migrations add`.

### Cambios posteriores al modelo

Si alguien necesita cambiar una entidad o el `DbContext`:

1. Avisar al equipo y pedirle a la persona encargada de migraciones que lo haga (o acordar quién la genera en ese momento).
2. Esa persona hace `git pull`, luego `dotnet ef migrations add NombreDescriptivo -p RuteaYa.Logica -s RuteaYa.Api`, commitea y pushea.
3. El resto hace `git pull` + `dotnet ef database update`.

### Problemas comunes

- **`dotnet ef` no se reconoce:** instalar la herramienta (`dotnet tool install --global dotnet-ef`) y reabrir la terminal.
- **"Unable to create a DbContext":** verificar que se ejecuta con `-s RuteaYa.Api` y que la connection string es válida.
- **No conecta a `(localdb)`:** ejecutar `sqllocaldb start MSSQLLocalDB` o usar otra instancia en `appsettings.Development.json`.
- **"The migration ... has already been applied":** hacer `git pull` para traer las migraciones nuevas antes de generar otra.

## Convenciones de Git

- Ramas: `main` (protegida), `develop` y `feature/HU-xx-descripcion`.
- Nada de push directo a `main`. Cada cambio entra por Pull Request con al menos una aprobación.
- PRs chicos y enfocados en una HU.

## Entidades y estados

- `EstadoEnvio`: BuscandoConductor, ConductorAsignado, Retirado, EnCamino, Entregado, Cancelado
- `EstadoViaje`: Activo, Inactivo
- `TamanoPaquete`: Pequeno, Mediano, Grande
- `DiasSemana`: enum de flags (Lunes, Martes, ... Domingo) que se guarda como un entero

Un envío solo puede asignarse una vez (índice único sobre `Asignaciones.EnvioId`).
