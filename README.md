# Appointment Booking & Token Queue API

Web API that lets an agency book appointments for customers, issue queue tokens,
and view the queue (grid of customers with appointments) for a given day.

## Stack
.NET 8, ASP.NET Core Web API, EF Core + SQLite, Serilog, Swagger (Swashbuckle),
xUnit + Moq, Docker, GitHub Actions.

## Solution structure

## Run locally
```bash
dotnet run --project src/Qwiik.Appointments.Api --urls http://localhost:5080
```
- Swagger UI: http://localhost:5080/swagger (the root URL redirects there)
- Health check: http://localhost:5080/health
- The SQLite database (`appointments.db`) is created and migrated on startup.

### Docker
```bash
docker build -t qwiik-appointments .
docker run --rm -p 8080:8080 -v qwiik-data:/data qwiik-appointments
```

### Tests
```bash
dotnet test
```

## Endpoints
| Method | Path | Purpose |
|---|---|---|
| POST | /api/appointments | Book an appointment and issue a token |
| GET | /api/queue?date=yyyy-MM-dd | Queue grid for a day, ordered by token (defaults to today) |
| PUT | /api/settings/max-per-day | Set the maximum appointments per day |
| POST | /api/off-days | Register an off day (e.g. public holiday) |
| GET | /health | Liveness plus database connectivity |

Example:
```bash
curl -X POST localhost:5080/api/appointments -H 'Content-Type: application/json' \
  -d '{"name":"Budi","phone":"0812","date":"2026-10-20"}'
```

## Business rules and assumptions
- Tokens start at 1 each day and are issued in booking order.
- Default maximum is 20 appointments per day; the agency can change it.
- If the requested day is an off day or is full, the booking moves to the next day
  that is open and has capacity (up to 365 days ahead). The response sets
  `movedFromRequestedDate: true`.
- Dates in the past are rejected (400). "Today" is evaluated in Asia/Jakarta.
- Out of scope: authentication, cancelling appointments (the status exists in the model).

## How the demonstration points are covered
| Point | Where |
|---|---|
| Separation of Business and Web layers | Core (business) / Infrastructure (data) / Api (web) |
| Third-party libraries via NuGet | EF Core, Serilog, Swashbuckle, xUnit, Moq |
| IoC/DI ready (e.g. Autofac) | Constructor injection against interfaces, registered in `Program.cs`; switching to Autofac only changes the registration |
| LINQ | Queue mapping in `AgencyService`, queries in the repositories |
| API documentation with Swagger | `/swagger` |
| Unit tests | `tests/`, run with `dotnet test` |
| Web API | Controllers under `src/Qwiik.Appointments.Api/Controllers` |
| Source control with GitHub | This repository; CI builds, tests and builds the Docker image on every push |
| Azure hosting | The app is container-ready (see Dockerfile). Deployment to Azure is not part of this submission. |

## Concurrency
Token assignment reads the last token for the day and adds one, so simultaneous bookings
can race. A unique index on `(Date, TokenNumber)` guarantees no duplicate tokens.
The service retries up to 5 times with a short randomized backoff; if conflicts persist
the API returns 409. Details and the incident write-up are in `RUNBOOK.md`.

## Known limitations
- SQLite allows a single writer; a production deployment should use a server database
  (e.g. Azure SQL) with serializable transactions or a per-day counter row.
- No authentication or rate limiting.
