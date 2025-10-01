# Models Directory Structure

This directory contains all the type definitions, interfaces, and enums for the Patient Scheduler application.

## 📁 Directory Structure

```
models/
├── interfaces/           # TypeScript interfaces
│   ├── patient.interface.ts
│   ├── doctor.interface.ts
│   ├── appointment.interface.ts
│   ├── invoice.interface.ts
│   ├── time-slot.interface.ts
│   ├── bulk-operation.interface.ts
│   ├── common.interface.ts
│   └── index.ts
├── enums/               # TypeScript enums
│   ├── appointment-status.enum.ts
│   ├── invoice-status.enum.ts
│   ├── bulk-operation-type.enum.ts
│   ├── job-status.enum.ts
│   └── index.ts
└── index.ts            # Main export file
```

## 🎯 Benefits

- **Modular Design**: Each interface and enum is in its own file for easy maintenance
- **Clean Imports**: Use `import { Patient, AppointmentStatus } from '../models'`
- **Type Safety**: Full TypeScript support with proper typing
- **Scalability**: Easy to add new interfaces and enums as the application grows
- **Organization**: Clear separation between interfaces and enums

## 📝 Usage Examples

```typescript
// Import specific interfaces
import { Patient, Doctor, Appointment } from '../models';

// Import enums
import { AppointmentStatus, InvoiceStatus } from '../models';

// Import everything from models
import * as Models from '../models';
```

## 🔄 Migration Notes

- All interfaces previously defined in `api.service.ts` have been moved to dedicated files
- The API service now imports from the models directory
- No breaking changes to existing functionality
