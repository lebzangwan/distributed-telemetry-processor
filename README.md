# High-Throughput Telemetry System

A resilient, decoupled, and dual-layered telemetry processing pipeline built with .NET 10, ASP.NET Core Web API, Background Workers, and SQL Server (`azure-sql-edge`).

## System Architecture Overview

The system is split into two independent services that communicate asynchronously to maximize processing throughput and eliminate data loss:

1. **Publisher Service (API)**: Generates simulated telemetry data every 1 second. It holds data in an in-memory `ConcurrentQueue` and spills data to SQL Server if capacity thresholds (10 items) or age limits (5 seconds) are exceeded.
2. **Consumer Service (Worker)**: Long-polls the Publisher API via an un-blocked background thread, offloads processing to a dedicated `System.Threading.Channels` pipeline, and saves calculated 5-point moving averages to the database.

## Quick Start (Docker Compose)

The entire environment—including database initialization and table schemas—spins up automatically using Docker Compose.

### Prerequisites
Ensure you have a `.env` file in your root folder with the following variables configured:
```env
DOCKER_DB_PASSWORD=YourSecureStrongPassword123!
DB_NAME=TelemetryDb
DB_USER=sa
ASPNETCORE_ENVIRONMENT=Development
```

### Spin Up the Application Stack
Run the following command from the repository root:
```bash
docker-compose up --build
```

### Port Mapping & Endpoints
* **Publisher API**: `http://localhost:5020`
  * Get Next Reading: `GET /api/telemetry/next`
  * System Statistics: `GET /api/telemetry/stats`
  * Health Check: `GET /api/telemetry/health`
* **Consumer Service**: Runs internally on port `5021` (polls the publisher automatically).
* **SQL Server**: `localhost:1433`

### Watch logs to watch Publisher generates data and Consumer process it:
* docker logs -f telemetry-publisher
* docker logs -f telemetry-consumer
