import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Appointment, BulkOperationRequest, JobStatus } from '../../models';

@Component({
  selector: 'app-bulk-operations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bulk-operations.component.html',
  styleUrls: ['./bulk-operations.component.css']
})
export class BulkOperationsComponent implements OnInit {
  appointments: Appointment[] = [];
  selectedAppointmentIds: number[] = [];
  jobs: JobStatus[] = [];
  
  // Form data
  operationType: string = 'BulkCancel';
  reason: string = '';
  newDateTime: string = '';
  billingAdjustment: number = 0;
  notes: string = '';
  
  // UI state
  showForm = false;
  loading = false;
  errorMessage = '';
  successMessage = '';
  
  // Job monitoring
  selectedJob: JobStatus | null = null;
  autoRefresh = true;
  refreshInterval: any;

  constructor(private apiService: ApiService) { }

  ngOnInit(): void {
    this.loadData();
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadData(): void {
    this.loadAppointments();
    this.loadJobs();
  }

  loadAppointments(): void {
    this.loading = true;
    this.apiService.getAppointments().subscribe({
      next: (appointments) => {
        this.appointments = appointments.filter(a => a.status === 'Scheduled');
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load appointments: ' + error.message;
        this.loading = false;
      }
    });
  }

  loadJobs(): void {
    this.apiService.getAllJobs().subscribe({
      next: (jobs) => {
        this.jobs = jobs;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load jobs: ' + error.message;
      }
    });
  }

  startAutoRefresh(): void {
    if (this.autoRefresh) {
      this.refreshInterval = setInterval(() => {
        this.loadJobs();
      }, 5000); // Refresh every 5 seconds
    }
  }

  stopAutoRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }

  toggleAutoRefresh(): void {
    this.autoRefresh = !this.autoRefresh;
    if (this.autoRefresh) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  showBulkForm(): void {
    this.showForm = true;
    this.clearMessages();
    this.selectedAppointmentIds = [];
  }

  cancelForm(): void {
    this.showForm = false;
    this.clearMessages();
    this.selectedAppointmentIds = [];
    this.resetForm();
  }

  resetForm(): void {
    this.operationType = 'BulkCancel';
    this.reason = '';
    this.newDateTime = '';
    this.billingAdjustment = 0;
    this.notes = '';
  }

  onAppointmentSelectionChange(appointmentId: number, event: Event): void {
    const target = event.target as HTMLInputElement;
    const isSelected = target.checked;
    
    if (isSelected) {
      if (!this.selectedAppointmentIds.includes(appointmentId)) {
        this.selectedAppointmentIds.push(appointmentId);
      }
    } else {
      this.selectedAppointmentIds = this.selectedAppointmentIds.filter(id => id !== appointmentId);
    }
  }

  selectAllAppointments(): void {
    this.selectedAppointmentIds = this.appointments.map(a => a.id!).filter(id => id !== undefined);
  }

  deselectAllAppointments(): void {
    this.selectedAppointmentIds = [];
  }

  onOperationTypeChange(): void {
    // Reset form fields when operation type changes
    this.reason = '';
    this.newDateTime = '';
    this.billingAdjustment = 0;
    this.notes = '';
  }

  submitBulkOperation(): void {
    if (this.selectedAppointmentIds.length === 0) {
      this.errorMessage = 'Please select at least one appointment.';
      return;
    }

    if (!this.operationType) {
      this.errorMessage = 'Please select an operation type.';
      return;
    }

    // Validate operation-specific requirements
    if (this.operationType === 'BulkReschedule' && !this.newDateTime) {
      this.errorMessage = 'Please select a new date and time for rescheduling.';
      return;
    }

    if (this.operationType === 'BulkCancel' && !this.reason.trim()) {
      this.errorMessage = 'Please provide a reason for cancellation.';
      return;
    }

    this.loading = true;
    this.clearMessages();

    const request: BulkOperationRequest = {
      operationType: this.operationType,
      appointmentIds: this.selectedAppointmentIds,
      reason: this.reason || undefined,
      newDateTime: this.newDateTime ? new Date(this.newDateTime) : undefined,
      billingAdjustment: this.billingAdjustment || undefined,
      notes: this.notes || undefined
    };

    this.apiService.queueBulkOperation(request).subscribe({
      next: (result) => {
        this.loading = false;
        this.successMessage = `Bulk operation queued successfully! Job ID: ${result.jobId}. Processing ${result.totalRecords} appointments.`;
        this.showForm = false;
        this.resetForm();
        this.selectedAppointmentIds = [];
        this.loadJobs(); // Refresh jobs list
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = 'Failed to queue bulk operation: ' + error.message;
      }
    });
  }

  viewJobDetails(job: JobStatus): void {
    this.selectedJob = job;
  }

  closeJobDetails(): void {
    this.selectedJob = null;
  }

  cancelJob(jobId: number): void {
    if (confirm('Are you sure you want to cancel this job?')) {
      this.apiService.cancelJob(jobId).subscribe({
        next: () => {
          this.successMessage = 'Job cancelled successfully.';
          this.loadJobs();
        },
        error: (error) => {
          this.errorMessage = 'Failed to cancel job: ' + error.message;
        }
      });
    }
  }

  getJobStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'queued': return 'status-queued';
      case 'processing': return 'status-processing';
      case 'completed': return 'status-completed';
      case 'failed': return 'status-failed';
      case 'cancelled': return 'status-cancelled';
      default: return 'status-unknown';
    }
  }

  getOperationTypeDisplay(type: string): string {
    switch (type) {
      case 'BulkCancel': return 'Bulk Cancel';
      case 'BulkReschedule': return 'Bulk Reschedule';
      case 'BulkBillingUpdate': return 'Bulk Billing Update';
      default: return type;
    }
  }

  getProgressPercentage(job: JobStatus): number {
    if (job.totalRecords === 0) return 0;
    return Math.round((job.processedRecords / job.totalRecords) * 100);
  }

  formatDuration(startTime?: Date, endTime?: Date): string {
    if (!startTime || !endTime) return 'N/A';
    const duration = new Date(endTime).getTime() - new Date(startTime).getTime();
    const minutes = Math.round(duration / 60000);
    return `${minutes} minutes`;
  }

  clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  getSuccessRate(job: JobStatus): number {
    return Math.round((job.processedRecords / job.totalRecords) * 100);
  }
}
