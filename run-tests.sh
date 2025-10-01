#!/bin/bash

echo "🧪 Running Patient Scheduler API Tests..."
echo "========================================"

# Navigate to the REST API directory
cd patient-scheduler-restapi

# Restore packages
echo "📦 Restoring packages..."
dotnet restore

# Build the project
echo "🔨 Building project..."
dotnet build

# Run tests
echo "🚀 Running tests..."
dotnet test --verbosity normal

echo "✅ Tests completed!"
