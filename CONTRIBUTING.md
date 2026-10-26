# Guía de contribución

## Ramas

- `main`: rama estable. Protegida, no se pushea directo.
- `develop`: integración. Acá se mergean las features.
- `feature/HU-xx-descripcion-corta`: una rama por historia de usuario (ej. `feature/HU-05-login`).

Flujo: crear la rama desde `develop` → trabajar → PR hacia `develop`. `develop` pasa a `main` solo en las entregas.

## Pull Requests

- Un PR por HU, chico y enfocado.
- Al menos **1 aprobación** de otro integrante antes de mergear.
- Antes de abrir el PR: `git pull origin develop`, resolver conflictos y verificar que la solución compile y los tests pasen.
- Título: `HU-xx: descripción corta`.
- Usar la plantilla de PR del repo.

## Commits

Mensajes cortos, en imperativo: `Agrega validación de email en registro`.

## Migraciones de base de datos

Solo **una persona** genera migraciones. El resto hace `git pull` y `dotnet ef database update`.
Ver el paso a paso en el [README](README.md).

## Capas y reglas

- `RuteaYa.Web` no referencia a otros proyectos y nunca usa EF ni `DbContext`. Consume la API por HTTP y usa sus propios ViewModels.
- La lógica de negocio vive en `RuteaYa.Logica`. No duplicarla en MVC ni en los controllers.
- Los controllers de la API son finos: reciben, llaman a un servicio y devuelven.
- Nunca exponer entidades por la API: usar DTOs.
- Usar `async/await` en toda operación de I/O.

## Convenciones de nombres

- Entidades y enums: singular, en español, sin tildes (`Envio`, `EstadoEnvio`).
- DTOs: sufijo `Request` / `Response` (`CrearEnvioRequest`, `EnvioResponse`).
- Interfaces con prefijo `I` (`IEnvioService`).

## Códigos HTTP de la API

| Situación | Código |
|---|---|
| OK / consulta | 200 |
| Creado | 201 |
| Datos inválidos / validación | 400 |
| No existe el recurso | 404 |
| Conflicto (ej. envío ya asignado, estado inválido) | 409 |
