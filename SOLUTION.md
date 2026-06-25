# High-Throughput Distributed Telemetry Processor

A production-grade, distributed microservice ecosystem designed to handle high-frequency sensor telemetry data under strict architectural constraints. The system separates network ingestion from calculation logic to ensure zero data loss while optimizing performance boundaries across high-throughput data processing lines.

---

## Architectural Blueprint

The system is split into two completely decoupled execution boundaries that communicate via an optimized HTTP REST pipeline, utilizing a single persistence layer for state preservation and historical analysis.

### 1. The Publisher (Web API Layer)
The Publisher handles incoming data streams. To survive massive spikes in telemetry frequency without memory crashes or thread starvation, it utilizes a dual-tier storage strategy:
* **In-Memory Buffer:** Uses a thread-safe `ConcurrentQueue` to accept and hold active records instantly within high-speed RAM.
* **Database Spillover Boundary:** If the in-memory queue depth exceeds 10 items, or if an item sits unconsumed for more than 5 seconds, an asynchronous background routine automatically moves the oldest records out of RAM and spills them into a persistent SQL table. This keeps the application's memory footprint incredibly light and stable.

### 2. The Consumer (Background Worker Service)
The Consumer operates as a continuous background daemon that pulls data from the Publisher for mathematical evaluation:
* **Decoupled Processing Pipeline:** Network polling and statistical math are separated using `System.Threading.Channels`. The main execution loop fetches records from the REST endpoint and drops them into an unallocated channel immediately, remaining highly responsive.
* **Complex Analysis Engine:** A dedicated, single-threaded consumer drains the channel sequentially, executing a rolling 5-Point Moving Average calculation per unique sensor type to smooth out erratic telemetry spikes before writing results to the database.

---

## Technical Performance Optimization

### 1. Boundary FIFO Guarantees & Eliminating Boilerplate with Global Exception Middleware 

* Cross Boundary FIFO Ordering Enforcement**: To avoid reading data out of order, the `DequeueAsync` task queries the SQL database first. Because items end up spilled into SQL Server strictly when memory overflows or ticks over maximum allowed wait age limits, SQL server logs represent the oldest known system information fragments. Querying the SQL table via index ordering before reading the in-memory array elements ensures a seamless FIFO sequence. In summary 
 - Database first execution prioritizes Data Consistency, guaranteed Strict FIFO & risk profile of minor latency overhead under low load VS 
 - Memory first that prioritizes throughput/speed, low database readson memory empty & risks data starvation inside dB tables.

* Rather than polluting repositories and controllers with repetitive, resource-expensive `try-catch` templates, the Web API leverages the modern `.NET 10` native `IExceptionHandler` model. 
* Any unexpected system anomalies (such as database connection drops or validation failures) bubble up cleanly to a centralized global error handler.
* The middleware intercepts the failure and outputs a professional, industry-standard **RFC 7807 Problem Details** JSON object to the client, masking internal infrastructure stack traces while keeping the codebase perfectly clean and readable.

### 2. Zero-Allocation Logging via Source Generators
In high-frequency systems, standard string-interpolated logging forces the runtime to allocate string objects on the managed heap continuously, triggering performance-killing Garbage Collection cycles. 
This solution uses C# compile-time **Logger Message Source Generators** (`[LoggerMessage]`). The compiler auto-generates strongly typed logging methods that check if a logging level is active *before* evaluating parameters, achieving near-zero memory allocation during critical data streams.

### 3. Resolving Distributed State Crashes
To ensure the system remains entirely stateless and scalable within Docker containers, all traditional integer counters and in-memory sequences were replaced with alphanumeric **GUIDs** (`Guid.NewGuid():N`). This eliminates primary key duplicate value constraints when containers restart or scale horizontally.

### 4. Usage of AI tools
AI tools were strategically used during development as an advanced pair programmer and static analysis sounding board. 
In thre following key areas:
* Micro-Performance Optimization: After writing the initial background worker service, I used AI to audit the loops for high-frequency performance bottlenecks. It helped me instantly implement Compile-Time Logger Message Source Generators ([LoggerMessage]) and swap out standard dictionary lookups for TryGetValue to avoid double-traversal overhead. This kept the code completely zero-allocation under heavy telemetry simulation loads.
* Refactoring & Code Hygiene: I used AI to ensure the project adhered strictly to the latest .NET best practices. It helped me clean up old-style collection initializations into modern C# clean collection expressions ([]), and ensure the global middleware seamlessly conformed to the RFC 7807 Problem Details standard for HTTP error responses.
* Infrastructure Automation: It helped accelerate the creation of the Docker orchestration layers—specifically tailoring the bash loops in the init-db.sh script to handle container race conditions and wait smoothly for the SQL Server engine health checks to pass before injecting the schema.

It allowed me to skip the tedious boilerplate setup and focus on what matters: data integrity, preserving strict FIFO sequencing, and establishing robust fault boundaries across distributed systems."

---

## Local Verification & Deployment Guide

This system is pre-configured to build, initialize, and execute instantly inside resource-constrained environments (such as an 8GB Intel Mac) using Docker container orchestration.

### Prerequisites
* Docker Desktop installed and running.

### 1. Run the Multi-Container Ecosystem
From the root directory of the project, execute the following command to prune stale caches, build the binaries, and spin up the architecture:

```
docker-compose down -v && docker builder prune -f && docker-compose up --build
```

### 2. Automated Database Initialization
You do not need to manually run schema scripts. The stack mounts an automated  script (`init-db.sh`) inside the database container. The script patiently waits for the SQL Server engine to complete internal booting sequences, verifies or creates the `TelemetryDb` catalog, and runs your `Database.sql` tables and Stored Procedures natively.

### 3. Live Log Inspection
To view the live, high-performance structured streaming logs from your background calculation engine, open a separate terminal window and run:

```
docker-compose logs -f consumer
```

### 4. Query System Statistics
To verify the internal stability, health, and spill states of the queue manager, query the Publisher stats endpoint directly from your host machine's terminal:

```
curl http://localhost:5020/api/telemetry/stats
```



