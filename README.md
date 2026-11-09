# Reservas Pro

API de reservas para negocios de servicios (barberías, spas, consultorios) hecha en **.NET 10**.
Calcula los turnos libres según los horarios del negocio y la duración de cada servicio, evita
reservas cruzadas y devuelve un enlace de WhatsApp para que el cliente confirme su cita.

Es la versión en .NET de un sistema de reservas propio que hoy funciona en producción para 3 negocios.

## Stack

- ASP.NET Core 10 (minimal APIs, validación integrada, ProblemDetails, rate limiting, OpenAPI)
- Entity Framework Core 10 + PostgreSQL
- xUnit para las reglas de negocio
- Dev container con PostgreSQL para GitHub Codespaces

## Arquitectura

```
src/
  ReservasPro.Domain/   Reglas de negocio puras, sin dependencias: Negocio, Servicio, Horario, Cita,
                        Agenda (turnos libres y reservas) y WhatsApp (enlace de confirmación).
  ReservasPro.Api/      Endpoints HTTP, EF Core y manejo de errores. Solo orquesta: la lógica vive en el dominio.
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
- **Errores claros.** Las reglas de negocio responden `400` o `409` en formato ProblemDetails; los errores inesperados,
  `500` sin detalles internos.

## Endpoints

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/negocios/{slug}` | Negocio con servicios activos y horarios |
| GET | `/api/negocios/{slug}/disponibilidad?servicioId=&fecha=` | Turnos libres de un servicio en una fecha |
| POST | `/api/negocios/{slug}/citas` | Reserva un turno y devuelve el enlace de WhatsApp |
| GET | `/health` | Estado del servicio |

Ejemplos listos para ejecutar en `src/ReservasPro.Api/ReservasPro.Api.http`.

## Ejecutar

### En GitHub Codespaces

Abre el repositorio en un Codespace: el contenedor trae .NET 10 y PostgreSQL ya conectados.

```bash
dotnet test
dotnet run --project src/ReservasPro.Api --launch-profile http
```

En desarrollo la API crea el esquema y un negocio de ejemplo (`barberia-centro`).

### En local

Necesitas el SDK de .NET 10 y un PostgreSQL. La cadena de conexión no va en el repositorio:

```bash
dotnet user-secrets set "ConnectionStrings:Reservas" "Host=localhost;Database=reservas;Username=..." --project src/ReservasPro.Api
dotnet run --project src/ReservasPro.Api --launch-profile http
```

## Próximos pasos

- Panel del dueño en Blazor (servicios, horarios y citas del día) con autenticación JWT
- Migraciones de EF Core para producción
- CI con GitHub Actions y despliegue en Azure
