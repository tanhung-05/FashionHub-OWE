#!/bin/bash
set -e

echo "Waiting for SQL Server to be ready..."
sleep 30s

echo "Running database migrations..."
cd /app
dotnet ef database update --no-build

echo "Database initialized successfully!"