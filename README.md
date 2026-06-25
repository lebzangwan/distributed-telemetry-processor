# High-Throughput Telemetry Processor

This repository contains a decoupled, distributed telemetry processing architecture built using **.NET 10** and **Docker**. The system is engineered to ingest high-frequency sensor streams safely without data loss, utilizing an automated database spillover strategy to protect system memory under heavy load.

---

## Architecture Components

* **Publisher (Web API):** Handles ingestion, maintains an in-memory concurrent queue, and automatically spills overflow records to SQL Server when thresholds are exceeded.
* **Consumer (Background Worker):** A background daemon using `System.Threading.Channels` to decouple network pulling from intense mathematical processing.
* **Database (SQL Server):** Fully automated relational persistence layer.

---

## Prerequisites

Before running the application, ensure you have the following installed on your machine:
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* Operating System: macOS, Windows, or Linux

---

## Quick Start Guide

Follow these steps to initialize, build, and execute the entire ecosystem cleanly.

### 1. Build and Launch the Ecosystem
Open your terminal at the root of the project directory and execute the following combined command. This ensures all historical database caches are pruned and the fresh binaries compile cleanly:

```bash
docker-compose down -v && docker builder prune -f && docker-compose up --build
```

### 2. Verify Database Auto-Setup
The system uses a built-in script (`init-db.sh`) to automatically start the database without errors. Just check the startup console to confirm that the database is running and the setup is complete.

### 3. View Live Execution Logs
To isolate and watch the background worker continuously running calculations and streaming GUID-backed records to persistence in real time, open a separate terminal tab and run:

```bash
docker-compose logs -f consumer
```

### 4. Check System Metrics
You can query the Publisher's live management endpoint to view the depth of the memory queue and see how many items have spilled over to the database layer under stress:

```bash
curl http://localhost:5020/api/telemetry/stats
```

### 5. Stopping the System
To gracefully stop all background threads, shut down the application containers, and clean up the virtual internal network safely, run:

```bash
docker-compose down
```

To stop the system and completely wipe the database clean (useful for a pristine reset before testing again), append the volume flag:

```bash
docker-compose down -v
```
