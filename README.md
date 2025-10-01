# Patient Scheduler Application

A comprehensive patient scheduling system with transactional operations, built with .NET 8 Web API and Angular frontend.

## 🏗️ Architecture

- **Backend**: .NET 8 Web API with Entity Framework Core (SQLite)
- **Frontend**: Angular 18 with TypeScript
- **Database**: SQLite (in-memory for development)
- **Containerization**: Docker & Docker Compose
- **API Documentation**: Swagger/OpenAPI

## 🚀 Quick Start

### Prerequisites

- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/install/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)
- [Node.js 20+](https://nodejs.org/) (for local development)
- [Angular CLI](https://angular.io/cli) (for local development)

### Running with Docker (Recommended)

1. **Clone and navigate to the project**:
   ```bash
   git clone <repository-url>
   cd patient-scheduler
   ```

2. **Build and start all services**:
   ```bash
   docker compose up --build -d
   ```

3. **Access the application**:
   - **Frontend**: http://localhost:4200
   - **API**: http://localhost:8080
   - **API Documentation (Swagger)**: http://localhost:8080/swagger

4. **Stop the services**:
   ```bash
   docker compose down
   ```

### Running Locally (Development)

#### Backend (API)

1. **Navigate to the API directory**:
   ```bash
   cd patient-scheduler-restapi
   ```

2. **Restore dependencies and run**:
   ```bash
   dotnet restore
   dotnet run
   ```

3. **Access the API**:
   - **API**: http://localhost:5000
   - **Swagger**: http://localhost:5000/swagger

#### Frontend (Angular)

1. **Navigate to the frontend directory**:
   ```bash
   cd patient-scheduler-webui
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Run the development server**:
   ```bash
   npm start
   # or
   ng serve
   ```

4. **Access the frontend**: http://localhost:4200

## 🧪 Testing

### Backend Tests

```bash
cd patient-scheduler-restapi
dotnet test
```

### Frontend Tests

```bash
cd patient-scheduler-webui
npm test
```

## 📁 Project Structure

```
patient-scheduler/
├── patient-scheduler-restapi/          # .NET 8 Web API
│   ├── Controllers/                    # API Controllers
│   ├── Models/                         # Data Models
│   ├── Services/                       # Business Logic
│   ├── Data/                          # Entity Framework Context
│   ├── DTOs/                          # Data Transfer Objects
│   ├── SQL/                           # Stored Procedures
│   └── PatientScheduler.Tests/        # Unit Tests
├── patient-scheduler-webui/           # Angular Frontend
│   ├── src/app/
│   │   ├── components/                # Angular Components
│   │   ├── models/                    # TypeScript Interfaces & Enums
│   │   │   ├── interfaces/            # TypeScript Interfaces
│   │   │   └── enums/                 # TypeScript Enums
│   │   └── services/                  # Angular Services
│   └── nginx.conf                     # Nginx Configuration
├── docker-compose.yml                 # Docker Compose Configuration
└── README.md                          # This file
```

## 🔧 Troubleshooting

### Angular CLI Issues

If you encounter the error `Cannot find module './bootstrap'` when running Angular commands:

**Solution 1: Use Docker (Recommended)**
```bash
docker compose up --build -d
```

**Solution 2: Clear and Reinstall Dependencies**
```bash
cd patient-scheduler-webui
rm -rf node_modules package-lock.json
npm install
```

**Solution 3: Use npx with Angular CLI**
```bash
cd patient-scheduler-webui
npx @angular/cli build
npx @angular/cli serve
```

**Solution 4: Install Angular CLI Globally**
```bash
npm install -g @angular/cli@latest
ng build
ng serve
```

### API Issues

**Swagger Not Accessible**: The API is configured to show Swagger in both Development and Production environments. If you can't access it, ensure the API is running on the correct port.

**Database Issues**: The application uses SQLite in-memory database. The database is recreated on each application start.

### Port Conflicts

If you encounter port conflicts:

- **API Port**: Change `8080` in `docker-compose.yml`
- **Frontend Port**: Change `4200` in `docker-compose.yml`

## 🚀 Features

### Core Functionality
- ✅ Patient Management (CRUD operations)
- ✅ Doctor Management
- ✅ Appointment Scheduling
- ✅ Time Slot Management
- ✅ Invoice Generation and Payment Processing
- ✅ Bulk Operations (Cancel, Reschedule, Billing Updates)

### Advanced Features
- ✅ Transactional Operations with Rollback Support
- ✅ Background Job Processing
- ✅ PDF Invoice Generation
- ✅ Comprehensive API Documentation (Swagger)
- ✅ Unit Testing (Backend)
- ✅ Modular TypeScript Architecture (Frontend)

### Bulk Operations
- **Bulk Cancel**: Cancel multiple appointments with automatic refunds
- **Bulk Reschedule**: Reschedule multiple appointments to a new date/time
- **Bulk Billing Update**: Update billing information for multiple appointments

## 🛠️ Development

### Adding New Features

1. **Backend**: Add controllers, services, and models in the `patient-scheduler-restapi` directory
2. **Frontend**: Add components and services in the `patient-scheduler-webui` directory
3. **Models**: Add new interfaces in `src/app/models/interfaces/` and enums in `src/app/models/enums/`

### API Endpoints

- **Patients**: `/api/patients`
- **Doctors**: `/api/doctors`
- **Appointments**: `/api/appointments`
- **Invoices**: `/api/invoices`
- **Time Slots**: `/api/timeslots`
- **Bulk Operations**: `/api/bulkoperations`

### Database Schema

The application uses Entity Framework Core with SQLite. Key entities:
- `Patient`: Patient information
- `Doctor`: Doctor information and specializations
- `Appointment`: Scheduled appointments
- `Invoice`: Billing and payment information
- `TimeSlot`: Available time slots
- `Job`: Background job tracking

## 📝 API Documentation

Once the application is running, access the interactive API documentation at:
- **Docker**: http://localhost:8080/swagger
- **Local**: http://localhost:5000/swagger


## 🏗️ Design Note: Architecture Decisions

**Why this approach was chosen:**

This approach prioritizes **separation of concerns** and **technology specialization** - leveraging .NET's robust transactional capabilities for complex business logic (appointment scheduling, billing, bulk operations) while utilizing Angular's reactive programming model for responsive user interfaces. The choice of SQLite in-memory database reflects a **development-first mindset**, enabling rapid iteration and testing without external database dependencies. The modular TypeScript architecture with centralized model exports (interfaces/enums) ensures **type safety** and **maintainability** across the frontend codebase.

**Trade-offs considered:**

While SQLite in-memory provides simplicity and speed for development, it sacrifices **data persistence** and **concurrent access** capabilities that would be required in production. The centralized model export pattern trades **explicit imports** for **convenience**, potentially creating hidden dependencies. The Docker-first deployment strategy prioritizes **environment consistency** but adds **infrastructure overhead** for simple development scenarios. The comprehensive Swagger documentation and transactional rollback support demonstrate a **reliability-first** approach, though this increases **code complexity** and **development time**.

**Bulk Operations & Rollback Architecture:**

The system implements a **hybrid approach** combining stored procedures with application-level transaction management for bulk operations. **Bulk Cancel**, **Bulk Reschedule**, and **Bulk Billing Update** operations use SQL stored procedures for **performance optimization** while maintaining **ACID compliance** through database-level transactions. The system validates **appointment state conditions** (ensuring appointments aren't already completed/cancelled) and **business rules** (preventing modifications to paid invoices) before execution. **Fallback mechanisms** automatically switch to individual transaction processing if bulk operations fail, ensuring **partial success handling** rather than complete failure. **Background job processing** with status tracking allows for **asynchronous execution** of large bulk operations, preventing UI blocking while maintaining **audit trails** through comprehensive logging. The **dual-layer rollback strategy** (database transactions + application-level compensation) provides **maximum data integrity** at the cost of increased **implementation complexity** and **performance overhead** for smaller operations.