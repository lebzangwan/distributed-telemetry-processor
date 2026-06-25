#!/bin/bash

set -e

echo "===================================================="
echo "Starting Database Initialization Script..."
echo "===================================================="

# Loop variables
max_attempts=30
attempt=1
db_ready=false

TARGET_HOST="sql-edge"

# Health check loop executing across the Docker network bridge
while [ "$attempt" -le "$max_attempts" ]; do
    echo "Testing SQL Server availability... (Attempt $attempt/$max_attempts...)"

    if /opt/mssql-tools/bin/sqlcmd -S "$TARGET_HOST" -U sa -P "${DOCKER_DB_PASSWORD}" -Q "SELECT 1" > /dev/null 2>&1; then
        echo "SQL Server is officially up and accepting connections!"
        db_ready=true
        break
    fi

    echo "SQL Server is still initializing..."

    sleep 5
    attempt=$((attempt + 1))
done

if [ "$db_ready" = false ]; then
    echo "ERROR: SQL Server failed to initialize within the timeout window. Exiting."
    exit 1
fi

echo "Executing Database Schema Creation..."

/opt/mssql-tools/bin/sqlcmd -S "$TARGET_HOST" -U sa -P "${DOCKER_DB_PASSWORD}" << EOF
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '${DB_NAME}') CREATE DATABASE ${DB_NAME};
EOF

echo "Database '${DB_NAME}' verified. Injecting tables and stored procedures..."
/opt/mssql-tools/bin/sqlcmd -S "$TARGET_HOST" -U sa -P "${DOCKER_DB_PASSWORD}" -d "${DB_NAME}" -i /app/Database.sql

echo "===================================================="
echo "Database Layer Initialized Successfully!"
echo "===================================================="