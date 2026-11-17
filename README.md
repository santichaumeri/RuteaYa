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

## 2. Crear la base de datos

**No hace falta hacer nada a mano.** Al iniciar `RuteaYa.Api` en modo Development (el modo por defecto de Visual Studio y de `dotnet run`), la API:

1. Aplica las migraciones pendientes. Si la base no existe, la crea.
2. Carga los [datos de prueba](#datos-de-prueba) si todavía no están.

Si preferís crear la base a mano (por ejemplo, para revisar las migraciones), desde la carpeta de la solución (donde está `RuteaYa.slnx`):

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

**Desde Visual Studio**

1. Clic derecho en la solución → *Configurar proyectos de inicio* → *Varios proyectos* → `RuteaYa.Api` y `RuteaYa.Web` en "Iniciar".
2. F5.

**Desde la terminal** (dos terminales, desde la carpeta de la solución):

```
dotnet run --project RuteaYa.Api --launch-profile https
```

```
dotnet run --project RuteaYa.Web --launch-profile https
```

| Qué | Dirección |
|---|---|
| Aplicación web (MVC) | https://localhost:7190 |
| API + Swagger | https://localhost:7001/swagger |

El MVC llama a la API en `https://localhost:7001/` (`ApiBaseUrl` en `RuteaYa.Web/appsettings.json`). Si cambiás el puerto de la API, cambialo también ahí.

En Swagger, los endpoints protegidos necesitan un token: hacé `POST /api/usuarios/login` con un usuario de prueba, copiá el `token` de la respuesta y pegalo en el botón **Authorize** (sin el prefijo `Bearer`).

## Datos de prueba

La primera vez que arranca la API en Development se cargan estos usuarios. **La contraseña de todos es `RuteaYa2026`.**

| Usuario | Email | Viajes |
|---|---|---|
| Carla Gómez (conductora) | `carla.conductora@ruteaya.test` | Moron → Ramos Mejia → CABA, de lunes a viernes · Castelar → Ituzaingo → Merlo, los sábados · Haedo → San Justo, inactivo |
| Diego Sosa (conductor) | `diego.conductor@ruteaya.test` | Ituzaingo → Castelar → Moron → Haedo, lunes, miércoles y viernes · CABA → Ramos Mejia → Moron, martes y jueves |
| Martina Ruiz (conductora) | `martina.conductora@ruteaya.test` | San Justo → Ramos Mejia → Haedo → Moron, lunes, miércoles y viernes · Padua → Ituzaingo → Castelar, los domingos |
| Ramiro Pérez (remitente) | `ramiro.remitente@ruteaya.test` | — |
| Lucía Fernández (remitente) | `lucia.remitente@ruteaya.test` | — |
| Sofía Álvarez (remitente) | `sofia.remitente@ruteaya.test` | — |
| Tomás Benítez (remitente) | `tomas.remitente@ruteaya.test` | — |
| Valeria Castro (remitente) | `valeria.remitente@ruteaya.test` | — |
| Julián Ortiz (remitente) | `julian.remitente@ruteaya.test` | — |

Las fechas se calculan desde el día en que se carga la base, así siempre hay envíos futuros. Para cada viaje activo se genera:

- **Envíos disponibles** para las próximas 3 semanas, en los días en que sale el viaje: de 1 a 3 por salida, y a veces uno que queda demasiado lejos (se publica, pero el viaje no lo ofrece). Diego y Martina también publican envíos, así se ve que el sistema no te ofrece los tuyos.
- **Transportes en curso:** el paquete de la última salida (retirado o en camino) y uno aceptado para cada una de las dos próximas salidas.
- **Historial** de los últimos 2 meses y medio: en un poco más de la mitad de las salidas el viaje llevó paquetes, ya entregados. También hay envíos cancelados.

Todo respeta las reglas de negocio: en cada fecha, ningún viaje lleva más paquetes, peso o desvío del que admite, nadie lleva sus propios envíos y el desvío guardado es el que calcula el servicio de rutas. Los datos se generan con una semilla fija, así que el mismo día producen siempre los mismos datos.

Además hay algunos envíos con nombre propio, pensados para probar casos concretos con **Carla**:

- **Oportunidades del viaje Moron → CABA**
  - El próximo lunes: "Documentación para firmar" (muy compatible), "Caja con repuestos" (compatible), "Juego de sábanas" (mucho desvío) y "Microondas". Junto con los generados, ese día hay más carga compatible de la que entra en el viaje (3 paquetes, 15 kg): sirve para probar el optimizador y los tres criterios.
  - Dos que **no** aparecen: "Libros" supera el desvío máximo y "Bicicleta" supera el peso máximo.
- **Oportunidades del viaje del sábado:** "Regalo de cumpleaños" y "Plantas".
- **Mis transportes:** "Notebook" con conductor asignado (para marcarlo como retirado) y "Medicamentos" en camino (para marcarlo como entregado).

La carga reconoce si los datos ya están (busca el usuario de Carla), así que no se duplican al reiniciar la API. Para volver a empezar desde cero, borrá la base y arrancá la API de nuevo:

```
dotnet ef database drop -p RuteaYa.Logica -s RuteaYa.Api
```

Para no cargar los datos de prueba, poné `"DatosIniciales": { "Cargar": false }` en `RuteaYa.Api/appsettings.Development.json`.

## Servicio de rutas

La distancia, el tiempo y el desvío de cada envío los calcula un servicio de rutas que está detrás de la interfaz `IRoutingService` (`RuteaYa.Logica/Servicios/Rutas`).

**No hace falta ninguna API key.** Por defecto se usa `MockRoutingService`, una implementación ficticia y determinística:

- Tiene cargadas distancias reales aproximadas entre Moron, Haedo, Castelar, Ituzaingo, Ramos Mejia, San Justo, Padua, Merlo, Ciudadela y CABA.
- Para cualquier otro par de lugares inventa una distancia fija entre 3 y 25 km, siempre la misma para el mismo par.
- Calcula el tiempo a 40 km/h.

Para usar un proveedor real (por ejemplo, Google Maps) habría que agregar una clase que implemente `IRoutingService` y registrarla en `RuteaYa.Api/Program.cs` cuando exista `GoogleMaps:ApiKey` en la configuración. El resto del sistema no cambia.

## Tests

Desde la carpeta de la solución:

```
dotnet test
```

Los tests (xUnit) no necesitan SQL Server: usan una base en memoria (EF Core InMemory), el mock de rutas y una fecha fija. Cubren las reglas de negocio: usuarios, viajes, envíos y sus estados, compatibilidad y oportunidades, aceptación, el optimizador y sus criterios, transportes, historial, potencial mensual y los datos de prueba.

En Visual Studio: *Prueba → Explorador de pruebas → Ejecutar todas las pruebas*.

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
- **El MVC muestra errores de conexión:** la API tiene que estar corriendo en la dirección de `ApiBaseUrl` (`RuteaYa.Web/appsettings.json`).
- **No aparecen los datos de prueba:** la API tiene que arrancar en Development (`ASPNETCORE_ENVIRONMENT=Development`), que es lo que usan los perfiles de `launchSettings.json`.
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
