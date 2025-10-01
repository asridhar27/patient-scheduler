import { Routes } from '@angular/router';
import { PatientManagementComponent } from './components/patient-management/patient-management.component';
import { AppointmentBookingComponent } from './components/appointment-booking/appointment-booking.component';
import { PaymentInvoiceComponent } from './components/payment-invoice/payment-invoice.component';
import { BulkOperationsComponent } from './components/bulk-operations/bulk-operations.component';

export const routes: Routes = [
  { path: '', redirectTo: '/patients', pathMatch: 'full' },
  { path: 'patients', component: PatientManagementComponent },
  { path: 'appointments', component: AppointmentBookingComponent },
  { path: 'invoices', component: PaymentInvoiceComponent },
  { path: 'bulk-operations', component: BulkOperationsComponent },
  { path: '**', redirectTo: '/patients' }
];