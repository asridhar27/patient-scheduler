import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Patient, Doctor, Appointment, TimeSlot } from '../../models';
import { PdfService, InvoiceData } from '../../services/pdf.service';

@Component({
  selector: 'app-appointment-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './appointment-booking.component.html',
  styleUrls: ['./appointment-booking.component.css']
})
export class AppointmentBookingComponent implements OnInit {
  patients: Patient[] = [];
  doctors: Doctor[] = [];
  appointments: Appointment[] = [];
  selectedDate: string = '';
  availableTimeSlots: TimeSlot[] = [];
  selectedTimeSlotId: number | null = null;
  
  newAppointment: Appointment = {
    patientId: 0,
    doctorId: 0,
    appointmentDateTime: '',
    duration: '01:00:00',
    notes: ''
  };
  
  selectedDateTime: Date | null = null;
  
  showForm = false;
  errorMessage = '';
  successMessage = '';
  selectedPatient: Patient | null = null;
  selectedDoctor: Doctor | null = null;
  loading = false;

  constructor(private apiService: ApiService, private pdfService: PdfService) { }

  ngOnInit(): void {
      this.loadData();
  }

  loadData(): void {
    this.loadPatients();
    this.loadDoctors();
    this.loadAppointments();
  }

  loadPatients(): void {
    this.apiService.getPatients().subscribe({
      next: (patients) => {
        this.patients = patients;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load patients: ' + error.message;
      }
    });
  }

  loadDoctors(): void {
    this.apiService.getDoctors().subscribe({
      next: (doctors) => {
        this.doctors = doctors;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load doctors: ' + error.message;
      }
    });
  }

  loadAppointments(): void {
    this.loading = true;
    this.apiService.getAppointments().subscribe({
      next: (appointments) => {
        this.appointments = appointments;
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load appointments: ' + error.message;
        this.loading = false;
      }
    });
  }

  showBookingForm(): void {
    this.newAppointment = {
      patientId: 0,
      doctorId: 0,
      appointmentDateTime: '',
      duration: '01:00:00',
      notes: ''
    };
    this.selectedPatient = null;
    this.selectedDoctor = null;
    this.selectedDate = '';
    this.availableTimeSlots = [];
    this.selectedTimeSlotId = null;
    this.showForm = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  onPatientChange(): void {
    this.selectedPatient = this.patients.find(p => p.id === this.newAppointment.patientId) || null;
  }

  onDoctorChange(): void {
    this.selectedDoctor = this.doctors.find(d => d.id === +this.newAppointment.doctorId) || null;
    console.log('Doctor changed:', this.selectedDoctor, 'DoctorId:', this.newAppointment.doctorId);
    this.tryLoadAvailableTimeSlots();
  }

  onDateChange(): void {
    this.tryLoadAvailableTimeSlots();
  }

  private tryLoadAvailableTimeSlots(): void {
    if (!this.newAppointment.doctorId || !this.selectedDate) {
      this.availableTimeSlots = [];
      this.selectedTimeSlotId = null;
      this.errorMessage = '';
      return;
    }

    this.apiService.getAvailableTimeSlots(this.newAppointment.doctorId, this.selectedDate).subscribe({
      next: (slots) => {
        this.availableTimeSlots = slots;
        this.selectedTimeSlotId = null;
        if (slots.length === 0) {
          this.errorMessage = 'No available slots for the selected doctor on this date.';
        } else {
          this.errorMessage = '';
        }
      },
      error: (error) => {
        this.errorMessage = 'Failed to load available time slots: ' + error.message;
        this.availableTimeSlots = [];
        this.selectedTimeSlotId = null;
      }
    });
  }

  bookAppointment(): void {
    if (!this.newAppointment.patientId || !this.newAppointment.doctorId || !this.selectedDate || !this.selectedTimeSlotId) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    if (this.availableTimeSlots.length === 0) {
      this.errorMessage = 'No available slots for the selected doctor on this date.';
      return;
    }

    // Set duration to 1 hour (default)
    this.newAppointment.duration = '01:00:00';

    const selectedSlot = this.availableTimeSlots.find(slot => slot.id === this.selectedTimeSlotId);
    if (!selectedSlot) {
      this.errorMessage = 'Please select a valid time slot.';
      return;
    }

    const selectedDateTime = new Date(selectedSlot.startTime);

    if (isNaN(selectedDateTime.getTime())) {
      this.errorMessage = 'Invalid date/time selected';
      return;
    }

    console.log('Selected slot startTime:', selectedSlot.startTime);
    console.log('Selected DateTime:', selectedDateTime);
    console.log('ISO String:', selectedDateTime.toISOString());

    const appointmentData = {
      ...this.newAppointment,
      appointmentDateTime: selectedDateTime.toISOString()
    };

    this.apiService.createAppointment(appointmentData).subscribe({
      next: (appointment) => {
        // Store the local selectedDateTime for display
        (appointment as any).selectedDateTime = selectedDateTime;
        this.appointments.push(appointment);
        this.showForm = false;
        this.successMessage = 'Appointment booked successfully!';
        this.loadAppointments(); // Refresh appointments list
      },
      error: (error) => {
        this.errorMessage = 'Failed to book appointment: ' + error.message;
      }
    });
  }

  bookAppointmentWithBilling(): void {
    if (!this.newAppointment.patientId || !this.newAppointment.doctorId || !this.selectedDate || !this.selectedTimeSlotId) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    if (this.availableTimeSlots.length === 0) {
      this.errorMessage = 'No available slots for the selected doctor on this date.';
      return;
    }

    if (!this.selectedDoctor) {
      this.errorMessage = 'Please select a doctor.';
      return;
    }

    // Set duration to 1 hour (default)
    this.newAppointment.duration = '01:00:00';

    // Calculate amount based on doctor's hourly rate (1 hour default)
    const amount = this.selectedDoctor.hourlyRate;
    const dueDate = new Date();
    dueDate.setDate(dueDate.getDate() + 30); // 30 days from now

    const selectedSlot = this.availableTimeSlots.find(slot => slot.id === this.selectedTimeSlotId);
    if (!selectedSlot) {
      this.errorMessage = 'Please select a valid time slot.';
      return;
    }

    const selectedDateTime = new Date(selectedSlot.startTime);

    if (isNaN(selectedDateTime.getTime())) {
      this.errorMessage = 'Invalid date/time selected';
      return;
    }

    console.log('Billing - Selected slot startTime:', selectedSlot.startTime);
    console.log('Billing - Selected DateTime:', selectedDateTime);
    console.log('Billing - ISO String:', selectedDateTime.toISOString());

    const appointmentData = {
      ...this.newAppointment,
      appointmentDateTime: selectedDateTime.toISOString()
    };

    this.apiService.scheduleAppointmentWithBilling(appointmentData, amount, dueDate.toISOString()).subscribe({
      next: (response: any) => {
        // Store the local selectedDateTime for display
        (response.appointment as any).selectedDateTime = selectedDateTime;
        this.appointments.push(response.appointment);
        this.showForm = false;
        
        if (response.invoice) {
          this.successMessage = `Appointment booked successfully with billing! 
            Amount: $${response.invoice.amount.toFixed(2)} 
            Status: ${response.invoice.status}
            Invoice ID: ${response.invoice.id}`;
            
          // Generate and download PDF invoice
          this.generateInvoicePdf(response.invoice, response.appointment);
        } else {
          this.successMessage = `Appointment booked successfully with billing! Amount: $${amount.toFixed(2)}`;
        }
        
        this.loadAppointments(); // Refresh appointments list
      },
      error: (error) => {
        this.errorMessage = 'Failed to book appointment with billing: ' + error.message;
      }
    });
  }

  cancelAppointment(appointment: Appointment): void {
    if (!appointment.id) return;

    if (confirm(`Are you sure you want to cancel this appointment?`)) {
      this.apiService.cancelAppointmentWithRefund(appointment.id).subscribe({
        next: () => {
          this.loadAppointments(); // Reload to get updated status
          this.successMessage = 'Appointment cancelled and refund processed!';
        },
        error: (error) => {
          this.errorMessage = 'Failed to cancel appointment: ' + error.message;
        }
      });
    }
  }

  cancelForm(): void {
    this.showForm = false;
    this.selectedPatient = null;
    this.selectedDoctor = null;
  }

  parseDuration(duration: string): number {
    const [hours, minutes] = duration.split(':').map(Number);
    return hours + (minutes / 60);
  }

  getAppointmentStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'status-scheduled';
      case 'completed': return 'status-completed';
      case 'cancelled': return 'status-cancelled';
      case 'noshow': return 'status-noshow';
      default: return 'status-unknown';
    }
  }

  getDisplayTime(appointment: any): string {
    if (appointment.originalSelectedTime) {
      return appointment.originalSelectedTime.toLocaleString('en-US', {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
    }
    return new Date(appointment.appointmentDateTime).toLocaleString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  private generateInvoicePdf(invoice: any, appointment: any): void {
    if (!this.selectedPatient || !this.selectedDoctor) {
      console.error('Missing patient or doctor information for PDF generation');
      return;
    }

    const invoiceData: InvoiceData = {
      id: invoice.id,
      amount: invoice.amount,
      status: invoice.status,
      dueDate: invoice.dueDate,
      paidDate: invoice.paidDate,
      notes: invoice.notes,
      createdAt: invoice.createdAt || new Date().toISOString(),
      patientName: `${this.selectedPatient.firstName} ${this.selectedPatient.lastName}`,
      doctorName: `Dr. ${this.selectedDoctor.firstName} ${this.selectedDoctor.lastName}`,
      appointmentDateTime: appointment.appointmentDateTime,
      patientEmail: this.selectedPatient.email,
      patientPhone: this.selectedPatient.phoneNumber,
      patientAddress: this.selectedPatient.address,
      doctorSpecialization: this.selectedDoctor.specialization
    };

    this.pdfService.generateInvoicePdf(invoiceData);
  }

}
