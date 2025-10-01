# Patient Scheduler API

A comprehensive patient scheduling system built with .NET Core that provides APIs for patient management, appointment scheduling, and invoice generation with transactional operations and rollback functionality.

## Features

- **Patient Management**: Create, read, update, and delete patient records
- **Appointment Scheduling**: Schedule appointments with doctors, including conflict detection
- **Invoice Generation**: Generate invoices for appointments with payment processing
- **Transactional Operations**: Handle multiple updates with rollback functionality
- **In-Memory Database**: Uses SQLite for data persistence
- **RESTful API**: Full CRUD operations with proper HTTP status codes
- **Error Handling**: Comprehensive error handling and validation
- **Swagger Documentation**: Interactive API documentation

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code (optional)

### Installation

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7000` (or the port shown in the console).

### Swagger Documentation

Once the application is running, visit `https://localhost:7000` to access the interactive Swagger documentation.

## API Endpoints

### Patients

- `GET /api/patients` - Get all patients
- `GET /api/patients/{id}` - Get a specific patient
- `POST /api/patients` - Create a new patient
- `PUT /api/patients/{id}` - Update a patient
- `DELETE /api/patients/{id}` - Delete a patient

### Appointments

- `GET /api/appointments` - Get all appointments
- `GET /api/appointments/{id}` - Get a specific appointment
- `POST /api/appointments` - Create a new appointment
- `POST /api/appointments/schedule-with-billing` - Schedule appointment with automatic billing
- `PUT /api/appointments/{id}` - Update an appointment
- `POST /api/appointments/{id}/cancel-with-refund` - Cancel appointment with automatic refund
- `DELETE /api/appointments/{id}` - Delete an appointment

### Invoices

- `GET /api/invoices` - Get all invoices
- `GET /api/invoices/{id}` - Get a specific invoice
- `POST /api/invoices` - Create a new invoice
- `PUT /api/invoices/{id}` - Update an invoice
- `POST /api/invoices/{id}/process-payment` - Process payment for an invoice
- `DELETE /api/invoices/{id}` - Delete an invoice

## Transactional Operations

The system includes several transactional operations that ensure data consistency:

### 1. Schedule Appointment with Billing

```http
POST /api/appointments/schedule-with-billing
Content-Type: application/json

{
  "patientId": 1,
  "doctorId": 1,
  "appointmentDateTime": "2024-01-15T10:00:00Z",
  "duration": "01:00:00",
  "notes": "Regular checkup"
}

Query Parameters:
- amount: 150.00
- dueDate: 2024-01-20T00:00:00Z
```

This operation:
1. Creates an appointment
2. Generates an invoice
3. If billing fails, rolls back the appointment creation

### 2. Process Payment

```http
POST /api/invoices/{id}/process-payment
```

This operation:
1. Updates invoice status to "Paid"
2. Sets the paid date
3. Uses transaction to ensure consistency

### 3. Cancel Appointment with Refund

```http
POST /api/appointments/{id}/cancel-with-refund
```

This operation:
1. Cancels the appointment
2. Processes refunds for any paid invoices
3. Uses transaction to ensure consistency

## Data Models

### Patient
- Id, FirstName, LastName, Email, PhoneNumber
- DateOfBirth, Address, MedicalHistory
- CreatedAt, UpdatedAt

### Doctor
- Id, FirstName, LastName, Specialization
- Email, PhoneNumber, OfficeAddress, HourlyRate
- CreatedAt, UpdatedAt

### Appointment
- Id, PatientId, DoctorId, AppointmentDateTime
- Duration, Notes, Status
- CreatedAt, UpdatedAt

### Invoice
- Id, PatientId, AppointmentId, Amount
- Status, DueDate, PaidDate, Notes
- CreatedAt, UpdatedAt

## Error Handling

The API includes comprehensive error handling:

- **400 Bad Request**: Invalid input data
- **404 Not Found**: Resource not found
- **409 Conflict**: Business rule violations (e.g., scheduling conflicts)
- **500 Internal Server Error**: Server-side errors

All errors include descriptive messages and proper HTTP status codes.

## Database

The application uses SQLite as the in-memory database with Entity Framework Core. The database file (`patientscheduler.db`) is created automatically when the application starts.

### Seed Data

The system includes pre-seeded doctor data:
- Dr. John Smith (Cardiology)
- Dr. Sarah Johnson (Pediatrics)
- Dr. Michael Brown (Orthopedics)

## Example Usage

### 1. Create a Patient

```bash
curl -X POST "https://localhost:7000/api/patients" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@email.com",
    "phoneNumber": "555-1234",
    "dateOfBirth": "1990-01-01T00:00:00Z",
    "address": "123 Main St",
    "medicalHistory": "No known allergies"
  }'
```

### 2. Schedule an Appointment with Billing

```bash
curl -X POST "https://localhost:7000/api/appointments/schedule-with-billing?amount=150.00&dueDate=2024-01-20T00:00:00Z" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": 1,
    "doctorId": 1,
    "appointmentDateTime": "2024-01-15T10:00:00Z",
    "duration": "01:00:00",
    "notes": "Regular checkup"
  }'
```

### 3. Process Payment

```bash
curl -X POST "https://localhost:7000/api/invoices/1/process-payment"
```

## Development

### Project Structure

```
PatientScheduler/
├── Controllers/          # API Controllers
├── Data/                # Entity Framework Context
├── DTOs/                # Data Transfer Objects
├── Models/              # Entity Models
├── Services/            # Business Logic Services
├── Program.cs           # Application Entry Point
└── PatientScheduler.csproj
```

### Adding New Features

1. Create new models in the `Models/` directory
2. Add corresponding DTOs in the `DTOs/` directory
3. Update the `PatientSchedulerContext` if needed
4. Create controllers in the `Controllers/` directory
5. Add business logic in the `Services/` directory

## License

This project is licensed under the MIT License.
