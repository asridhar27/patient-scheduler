# Troubleshooting Guide

## 🚨 Common Issues and Solutions

### Angular CLI Bootstrap Error

**Error**: `Cannot find module './bootstrap'`

This is a common Angular CLI environment issue that can occur after Node.js updates or dependency changes.

#### Solutions (in order of preference):

**1. Use Docker (Recommended)**
```bash
# This bypasses all local environment issues
docker compose up --build -d
```

**2. Clear Node Modules and Reinstall**
```bash
cd patient-scheduler-webui
rm -rf node_modules package-lock.json
npm install
npm start
```

**3. Use npx with Angular CLI**
```bash
cd patient-scheduler-webui
npx @angular/cli@latest serve
npx @angular/cli@latest build
```

**4. Install Angular CLI Globally**
```bash
npm install -g @angular/cli@latest
ng serve
ng build
```

**5. Check Node.js Version**
```bash
node --version  # Should be 18+ (20+ recommended)
npm --version   # Should be 9+
```

### API Not Accessible

**Issue**: Cannot access API endpoints or Swagger documentation

**Solutions**:
1. Check if the API is running:
   ```bash
   curl http://localhost:8080/health  # Docker
   curl http://localhost:5000/health  # Local
   ```

2. Verify port configuration in `docker-compose.yml`

3. Check Docker container status:
   ```bash
   docker compose ps
   ```

### Frontend Not Loading

**Issue**: Angular app not loading or showing errors

**Solutions**:
1. Check if both services are running:
   ```bash
   docker compose ps
   ```

2. Check container logs:
   ```bash
   docker compose logs patient-scheduler-webui
   docker compose logs patient-scheduler-restapi
   ```

3. Restart containers:
   ```bash
   docker compose restart
   ```

### Database Issues

**Issue**: Data not persisting or database errors

**Note**: The application uses SQLite in-memory database, so data is reset on each restart.

**Solutions**:
1. This is expected behavior for development
2. For production, configure a persistent database in `Program.cs`

### Port Conflicts

**Issue**: Port already in use

**Solutions**:
1. Change ports in `docker-compose.yml`:
   ```yaml
   ports:
     - "8081:8080"  # Change 8080 to 8081
     - "4201:80"    # Change 4200 to 4201
   ```

2. Kill processes using the ports:
   ```bash
   lsof -ti:8080 | xargs kill -9
   lsof -ti:4200 | xargs kill -9
   ```

### Build Failures

**Issue**: Docker build fails

**Solutions**:
1. Clear Docker cache:
   ```bash
   docker system prune -a
   docker compose build --no-cache
   ```

2. Check Dockerfile syntax and dependencies

### TypeScript Compilation Errors

**Issue**: TypeScript errors after model restructuring

**Solutions**:
1. All model references have been updated
2. Check imports in components:
   ```typescript
   import { Patient, Doctor } from '../models';  // ✅ Correct
   import { Patient } from '../services/api.service';  // ❌ Wrong
   ```

3. Verify model files exist in `src/app/models/`

## 🔧 Development Environment Setup

### Clean Installation

1. **Remove all dependencies**:
   ```bash
   cd patient-scheduler-webui
   rm -rf node_modules package-lock.json
   cd ../patient-scheduler-restapi
   rm -rf bin obj
   ```

2. **Rebuild everything**:
   ```bash
   cd ..
   docker compose down
   docker compose up --build -d
   ```

### Local Development Setup

1. **Backend**:
   ```bash
   cd patient-scheduler-restapi
   dotnet restore
   dotnet build
   dotnet run
   ```

2. **Frontend** (if Angular CLI works):
   ```bash
   cd patient-scheduler-webui
   npm install
   npm start
   ```

## 📞 Getting Help

If you're still experiencing issues:

1. **Check the logs**:
   ```bash
   docker compose logs -f
   ```

2. **Verify system requirements**:
   - Docker 20.10+
   - Node.js 18+ (20+ recommended)
   - .NET 8 SDK

3. **Test individual components**:
   - API health check: `curl http://localhost:8080/health`
   - Frontend static files: Check browser network tab

4. **Use Docker for development**: This is the most reliable approach and avoids most environment issues.
