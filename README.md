# Reservas Pro

Sistema de reservas para negocios de servicios (barberías, spas, consultorios) hecho en **.NET 10**:
una API que calcula los turnos libres, evita reservas cruzadas y devuelve un enlace de WhatsApp para
confirmar la cita, y un panel en **Blazor WebAssembly** donde el dueño administra citas, servicios y horarios.

Es la versión en .NET de un sistema de reservas propio que hoy funciona en producción para 3 negocios.

## Stack

- ASP.NET Core 10: minimal APIs, validación integrada, ProblemDetails, rate limiting, OpenAPI
- Autenticación JWT (firmados por la API) con contraseñas hasheadas con `PasswordHasher` de ASP.NET Core Identity
- Entity Framework Core 10 + PostgreSQL
- Blazor WebAssembly para el panel del dueño
- xUnit para las reglas de negocio
- Dev container con PostgreSQL para GitHub Codespaces

## Arquitectura

```
src/
  ReservasPro.Domain/      Reglas de negocio puras, sin dependencias: Negocio, Servicio, Horario, Cita, Usuario,
                           Agenda (turnos libres y reservas) y WhatsApp (enlace de confirmación).
  ReservasPro.Contracts/   DTOs compartidos por la API y el panel (mismas validaciones en ambos lados).
  ReservasPro.Api/         Endpoints HTTP, EF Core, JWT y manejo de errores. Solo orquesta: la lógica vive en el dominio.
  ReservasPro.Panel/       Panel del dueño en Blazor WebAssembly.
tests/
  ReservasPro.Domain.Tests/   Pruebas de las reglas de la agenda.
```

Decisiones principales:

- **La agenda es una función pura.** `Agenda.TurnosLibres` recibe el negocio, el servicio, la fecha, las citas
  y el instante actual; por eso se prueba sin base de datos ni reloj real.
- **Hora local del negocio.** Horarios y citas se guardan en la zona horaria del negocio (`DateOnly` + `TimeOnly`);
  el instante actual llega en UTC y se convierte, así "hoy" es el hoy del negocio aunque el servidor esté en otro país.
- **Sin reservas dobles.** La reserva corre en una transacción `Serializable`: si dos clientes piden el mismo turno
  a la vez, PostgreSQL rechaza una y la API responde `409 Conflict`.
- **Cada dueño solo ve su negocio.** El JWT lleva el negocio del usuario y todos los endpoints del panel filtran por
  él; cambiar un id en la URL de otro negocio da `404`.
- **Login resistente a ataques simples.** Límite de 5 intentos por minuto por IP y verificación de hash aunque el correo
  no exista, para que el tiempo de respuesta no delate qué cuentas hay.
- **Sin secretos en el repositorio.** La cadena de conexión y la clave de los JWT van en user-secrets en desarrollo y
  en variables de entorno o Key Vault en producción. El dueño de ejemplo recibe una contraseña aleatoria que solo se
  muestra en la consola de desarrollo.
- **Token del panel en `sessionStorage`.** Sobrevive a recargar la página y se borra al cerrar la pestaña; dura 8 horas.

## Endpoints

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/negocios/{slug}` | Negocio con servicios activos y horarios |
| GET | `/api/negocios/{slug}/disponibilidad?servicioId=&fecha=` | Turnos libres de un servicio en una fecha |
| POST | `/api/negocios/{slug}/citas` | Reserva un turno y devuelve el enlace de WhatsApp |
| POST | `/api/auth/login` | Inicia sesión del dueño y devuelve el JWT |
| GET | `/api/panel/citas?fecha=` | Citas del día (requiere JWT) |
| POST | `/api/panel/citas/{id}/cancelar` | Cancela una cita |
| GET · POST | `/api/panel/servicios` | Lista o crea servicios |
| POST | `/api/panel/servicios/{id}/desactivar` | Deja de ofrecer un servicio |
| GET · POST | `/api/panel/horarios` | Lista o crea franjas de atención |
| DELETE | `/api/panel/horarios/{id}` | Quita una franja |
| GET | `/health` | Estado del servicio |

Ejemplos listos para ejecutar en `src/ReservasPro.Api/ReservasPro.Api.http`.

## Ejecutar

### En GitHub Codespaces

Abre el repositorio en un Codespace: el contenedor trae .NET 10 y PostgreSQL conectados y genera la clave de los JWT.

```bash
dotnet test
dotnet run --project src/ReservasPro.Api --launch-profile http
```

La API sirve también el panel en la misma dirección (puerto 5015). En desarrollo aplica las migraciones, crea
un negocio de ejemplo (`barberia-centro`) y escribe en la consola el usuario y la contraseña del dueño.

### En local

Necesitas el SDK de .NET 10 y un PostgreSQL. Los secretos no van en el repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:Reservas" "Host=localhost;Database=reservas;Username=..." --project src/ReservasPro.Api
dotnet user-secrets set "Jwt:Clave" "<al menos 32 caracteres aleatorios>" --project src/ReservasPro.Api
```

## CI/CD

- **CI** ([`ci.yml`](.github/workflows/ci.yml)): en cada push y pull request revisa paquetes con vulnerabilidades
  conocidas, compila con las advertencias como errores, ejecuta las pruebas y verifica que las migraciones estén al
  día con el modelo. Las acciones de GitHub van fijadas por commit.
- **Despliegue** ([`desplegar.yml`](.github/workflows/desplegar.yml)): API en Azure App Service y panel en Azure
  Static Web Apps, con inicio de sesión OIDC (sin contraseñas de Azure en GitHub) y secretos en Key Vault.
  Paso a paso en [docs/DESPLIEGUE.md](docs/DESPLIEGUE.md).

## Próximos pasos

- Alta del primer negocio en producción
- Página pública de reservas
