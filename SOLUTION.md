# Architectural Design Review & Solution Implementation

This document details the engineering decisions, concurrency controls, and optimization strategies applied to satisfy the high-throughput telemetry requirements.

---

## 1. Core Goal Achievement Verification

The primary goal of building a decoupled, multi-threaded, and threshold-backed telemetry ingestion engine **has been successfully achieved**. 

### Architectural Compliance Checklist
* **Publisher Time-Based Spill**: The engine monitors timestamps. If an item sits unconsumed in RAM for >5 seconds, it spills to the database immediately to save memory.
* **Publisher Capacity-Based Spill**: When the in-memory queue crosses the threshold of 10 elements, the oldest item spills immediately to disk via an asynchronous database write thread.
* **Strict FIFO Preservation**: The retrieval path prioritizes the active in-memory cache first to guarantee high performance, falling back to the database queue ONLY when RAM is dry.
* **Non-Blocking Consumer Core**: The consumer uses a dedicated background thread to poll the API, completely offloading calculation tasks to an isolated Processing Channel.

---

## 2. Key Engineering Implementations

### A. Thread-Safe Concurrency & State Isolation
* **Publisher Memory Safety**: Used `ConcurrentQueue<(SensorReading Reading, DateTime EnqueuedAt)>` combined with `Interlocked` atomic counters (`Interlocked.Increment`) to prevent data corruption during simultaneous multi-threaded reads/writes.
* **Consumer Channel Architecture**: Leveraged `System.Threading.Channels.CreateUnbounded<SensorReading>` with explicit configurations (`SingleReader = true`, `SingleWriter = true`). This yields maximum throughput, zero thread-locking contention, and zero CPU polling overhead.

### B. High-Performance Logging
* Replaced standard C# string interpolation logging with compile-time source-generated **`[LoggerMessage]` extensions**. This eliminates object allocation and string parsing overhead entirely on the telemetry collection loop paths.

### C. Bulletproof Database Layer (Concurrency Fixes)
* To support potential horizontal scaling of the consumer services, the `sp_GetOldestPending` stored procedure implements advanced T-SQL transaction isolation locking. 
* By wrapping the fetch in a `WITH (ROWLOCK, UPDLOCK, READPAST)` block, multiple instances of the service can pull from the database at the exact same moment without causing deadlocks or processing duplicate records.
* Also note that init-sb.sh logs into the `master` context first before dynamically targeting the database.

### D. Production-Grade Exception Management
* Built a modern .NET 8 standard `IExceptionHandler` pipeline. It intercepts application faults globally and maps them cleanly into RFC 7807 `ProblemDetails` compliance documents.
* Security enhancements ensure that raw database connection strings or structural system faults are obscured in Production while returning highly traceable tracking codes (`traceId`).

---

## 3. Edge-Case Resolution & Optimizations Applied

During development, two major architectural bottlenecks were identified and refactored to achieve quality compliance:

### 1. Correcting the Memory Retrieval Order
* **The Vulnerability**: Checking the database before checking the in-memory queue under high-load shifts the application into a disk-heavy pass-through pipeline. Data sits trapped in memory until it times out.
* **The Fix**: Rewrote `TelemetryQueueManager.DequeueAsync` to extract data from RAM first. This preserves real-time response latency and only falls back to querying disk infrastructure during low-traffic periods.

### 2. Eliminating Container Boot Race Conditions
* **The Vulnerability**: If the consumer service initializes before the publisher API container is fully ready, it hits early network connection failure loops.
* **The Fix**: Configured advanced multi-stage health checks. The database schema manager waits for `sql-edge` to report healthy, while the consumer container relies on a strict `condition: service_started` dependency tied to the publisher API container.